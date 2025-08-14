using System;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering;


[System.Serializable]
public class FogOfWarStatus
{
    public FogOfWarViewStatus status;
    public int viewType = 0;
}

public enum FogOfWarViewStatus
{
    Hidden = 0,
    Visited,
    Visible,
    Blocked
}

public class FOW : MonoBehaviour, IObserver
{

    private GameManager gameManager;

    private FogOfWarStatus[,] fogOfWarStatuses = new FogOfWarStatus[0, 0];
    private Texture2D fogTexture;
    public Material fogMaterial;
    [SerializeField]
    private Shader fowShader;
    private Vector4[] _revealerPosition;
    private float[] _revealerRadii;
    private RenderTexture totalTexture;
    private RenderTexture[] visitedTexture;
    private RenderTexture[] renderTextures;


    public int tileWidthCount;
    public int tileHeightCount;

    private const int RunnerLayer = 8;
    private const int ChaserLayer = 9;
    private const int VisibleLayer = 10;

    public CharacterController[] Characters;
    private byte maxPlayers = 2;
    [SerializeField]
    private Transform[] characterTf;
    protected Vector4[] _revealerPositionArray;

    public float viewAngle;
    private float viewRad;
    [SerializeField] private TMP_Dropdown dropdown;
    private byte nowPlayer;
    [SerializeField]
    private Camera cam;

    private const int MAX_REVEALERS = 100;


    public void Init(int _tileWidthCount, int _tileHeightCount)
    {
        gameManager = GameManager.Instance;
        characterTf = new Transform[maxPlayers];
        tileWidthCount = _tileWidthCount;
        tileHeightCount = _tileHeightCount;
        renderTextures = new RenderTexture[maxPlayers];
        _revealerPosition = new Vector4[MAX_REVEALERS];
        _revealerRadii = new float[MAX_REVEALERS];
        totalTexture = new RenderTexture(ConstVariables.Resolution, ConstVariables.Resolution, 0, RenderTextureFormat.ARGB32);
        visitedTexture = new RenderTexture[maxPlayers];
        for (int i = 0; i < maxPlayers; i++)
        {
            visitedTexture[i] = new RenderTexture(ConstVariables.Resolution, ConstVariables.Resolution, 0, RenderTextureFormat.ARGB32);
        }
        CreateFogTexture();
        cam = Camera.main;
        fogOfWarStatuses = new FogOfWarStatus[tileWidthCount, tileHeightCount];
        cam.depthTextureMode = DepthTextureMode.Depth;
        for (var i = 0; i < tileHeightCount; i++)
        {
            for (var j = 0; j < tileWidthCount; j++)
            {
                fogOfWarStatuses[j, i] = new FogOfWarStatus();

                fogOfWarStatuses[j, i].status = FogOfWarViewStatus.Hidden;
            }
        }

        characterTf[0] = Characters[0].transform;
        characterTf[1] = Characters[1].transform;
        viewRad = viewAngle * Mathf.Deg2Rad;
        UpdateFogTexture();
        OnValueChange();
    }

    private void LateUpdate()
    {
        ShowFOWView();
    }



    private void ShowFOWView()
    {

        switch (nowPlayer)
        {
            case 2:
                {
                    ShowAllUnitView();
                    return;
                }
            default:
                {
                    int sightRadius = 0;
                    Vector2Int position = new Vector2Int(0, 0);
                    sightRadius = Characters[nowPlayer].GetSightDistance();
                    short count = 0;
                    if (Characters[nowPlayer].GetMovementCount() > 0)
                    {
                        count = (short)(Characters[nowPlayer].GetMovementCount() - 1);
                    }

                    position = Characters[nowPlayer].Astar.CurrentNode.Position;
                    UpdateFogOfWarStatus(position, sightRadius);
                    break;
                }
        }

    }
    private void CreateFogTexture()
    {
        fogTexture = new Texture2D(tileWidthCount, tileHeightCount, TextureFormat.Alpha8, false);
        fogTexture.filterMode = FilterMode.Point;
        fogTexture.wrapMode = TextureWrapMode.Clamp;

        Color32[] initialColors = new Color32[tileWidthCount * tileHeightCount];
        for (int i = 0; i < initialColors.Length; i++)
        {
            initialColors[i] = new Color32(0, 0, 0, 255);
        }
        fogTexture.SetPixels32(initialColors);
        fogTexture.Apply();
        if (fogMaterial != null)
        {
            fogMaterial.mainTexture = fogTexture;
            fogMaterial.mainTextureScale = new Vector2(-1, -1);
            fogMaterial.mainTextureOffset = new Vector2(1, 1);

        }
    }

    private void UpdateFogTexture()
    {
        Color32[] colors = new Color32[tileWidthCount * tileHeightCount];
        int visibleCount = 0;
        for (int y = 0; y < tileHeightCount; y++)
        {
            for (int x = 0; x < tileWidthCount; x++)
            {
                int index = y * tileWidthCount + x; // 1D 인덱스로 변환
                if (nowPlayer == 2)
                {
                    continue;
                }
                if (fogOfWarStatuses[x, y].viewType == nowPlayer + 1 || fogOfWarStatuses[x, y].viewType == 3)
                {
                    continue;
                }
                fogOfWarStatuses[x, y].status = FogOfWarViewStatus.Hidden;
                visibleCount = SetVisiblity(fogOfWarStatuses[x, y].status, colors, index);
            }
        }
        fogTexture.SetPixels32(colors);
        fogTexture.Apply(); // 변경사항 GPU에 적용
    }

    private int SetVisiblity(FogOfWarViewStatus status, Color32[] colors, int index)
    {
        int visibleCount = 0;
        switch (status)
        {

            case FogOfWarViewStatus.Visited:
                {
                    colors[index] = new Color32(0, 0, 0, 254); // 검은색, 반투명 (회색 느낌)

                    break;
                }

            case FogOfWarViewStatus.Visible:
                {
                    colors[index] = new Color32(0, 0, 0, 0); // 완전 투명
                    visibleCount++;
                    break;
                }

            default:
                {
                    colors[index] = new Color32(0, 0, 0, 255); // 검은색, 완전 불투명
                    break;
                }
        }

        return visibleCount;
    }

    #region Rendering Type FOW
    public void OnNotify(byte eventType, object data)
    {
        switch (eventType)
        {
            case ConstDataType.fowSight:
                {

                    break;
                }
        }
    }

    public void OnNotify(byte eventType, object data0, object data1)
    {
        
        switch (eventType)
        {
            case ConstDataType.fowSight:
                {
                    _revealerPosition[(byte)data0 - 8] = characterTf[(byte)data0 - 8].position; // 레이어 기준으로 구분
                    renderTextures[(byte)data0 - 8] = (RenderTexture)data1; // 렌더 텍스쳐 설정
                    SetRenderImage((byte)data0);
                    break;
                }
            default:
                {
                    break;
                }
        }
    }

    protected void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        fogMaterial.SetMatrix("_CameraToWorldMatrix", cam.cameraToWorldMatrix);
        Graphics.Blit(source, destination, fogMaterial);
    }

    protected void SetRenderImage(byte _layer)
    {
        fogMaterial.SetVectorArray("_RevealerPosition", _revealerPosition);
        fogMaterial.SetFloat("_RevealerRadius", viewAngle);

    }

    
    #endregion


    private void UpdateFogOfWarStatus(Vector2Int position, int sightRadius)
    {
        for (int x = 0; x < tileWidthCount; x++)
        {
            for (int y = 0; y < tileHeightCount; y++)
            {
                if (fogOfWarStatuses[x, y].status != FogOfWarViewStatus.Visible)
                {
                    continue;
                }
                fogOfWarStatuses[x, y].status = FogOfWarViewStatus.Visited;
            }
        }
        ShowUnitView(position, sightRadius, (byte)(nowPlayer + 1));
        ShowUnitView(Characters[1 - nowPlayer].Astar.CurrentNode.Position, sightRadius, (byte)(2 - nowPlayer));
        UpdateFogTexture();
    }

    private void ShowAllUnitView()
    {
        for (int x = 0; x < tileWidthCount; x++)
        {
            for (int y = 0; y < tileHeightCount; y++)
            {
                if (fogOfWarStatuses[x, y].status != FogOfWarViewStatus.Visible)
                {
                    continue;
                }

                fogOfWarStatuses[x, y].status = FogOfWarViewStatus.Visited;
            }
        }

        for (int i = 0; i < Characters.Length; i++)
        {


            Vector2Int position = Characters[i].Astar.CurrentNode.Position;
            int sightRadius = Characters[i].GetSightDistance();
            ShowUnitView(position, sightRadius, (byte)(i + 1));
        }

        UpdateFogTexture();

    }

    private void ShowUnitView(Vector2Int position, int sightRadius, byte playerNum)
    {
        for (int x = position.x - sightRadius; x <= position.x + sightRadius; x++)
        {
            for (int y = position.y - sightRadius; y <= position.y + sightRadius; y++)
            {
                // 맵 범위 체크
                if (x < 0 || x >= tileWidthCount || y < 0 || y >= tileHeightCount)
                    continue;

                Vector2Int targetTilePos = new Vector2Int(x, y);
                float distance = Vector2Int.Distance(position, targetTilePos);

                if (distance > sightRadius)
                {
                    continue;

                }

                byte currentNum = (byte)(playerNum - 1);
                if (!HasLineOfSight(position, targetTilePos, characterTf[currentNum]))
                {
                    continue;
                }
                if (fogOfWarStatuses[x, y].viewType == 0)
                {
                    fogOfWarStatuses[x, y].viewType = playerNum;
                }
                else if (fogOfWarStatuses[x, y].viewType == 2 - currentNum)
                {
                    fogOfWarStatuses[x, y].viewType = 3;
                }
                if (currentNum != nowPlayer && nowPlayer != 2)
                {
                    continue;
                }
                // Line of Sight (LOS) 검사
                fogOfWarStatuses[x, y].status = FogOfWarViewStatus.Visible;




            }
        }
    }


    private bool HasLineOfSight(Vector2Int startTile, Vector2Int endTile, Transform nowCharacterTf)
    {
        Vector3 startWorldPos = GetWorldPositionFromTile(startTile) + Vector3.up * 0.5f; // 눈높이
        Vector3 endWorldPos = GetWorldPositionFromTile(endTile) + Vector3.up * 0.5f; // 타일 중심 약간 위

        Vector3 direction = endWorldPos - startWorldPos;
        direction.Normalize();

        float dotProduct = Vector3.Dot(nowCharacterTf.forward, direction);
        float minDotProduct = Mathf.Cos(viewAngle * 0.5f * Mathf.Deg2Rad);
        if (dotProduct < minDotProduct)
        {
            return false;
        }


        RaycastHit hit;
        if (Physics.Linecast(startWorldPos, endWorldPos, out hit, LayerMask.GetMask("Wall")))
        {
            return false;
        }
        return true;
    }


    Vector3 GetWorldPositionFromTile(Vector2Int tilePos)
    {
        if (gameManager.tiling.IsUnityNull())
        {
            return Vector3.zero;
        }

        if (gameManager.tiling.Tiles == null)
        {
            return Vector3.zero;
        }

        if (tilePos.x < 0)
        {
            return Vector3.zero;
        }

        if (tilePos.x >= gameManager.tiling.Tiles.GetLength(0))
        {
            return Vector3.zero;
        }

        if (tilePos.y < 0)
        {
            return Vector3.zero;
        }

        if (tilePos.y >= gameManager.tiling.Tiles.GetLength(1))
        {
            return Vector3.zero;
        }


        return gameManager.tiling.Tiles[tilePos.x, tilePos.y].position;

    }


    #region Dropdown Event
    public void OnValueChange()
    {
        nowPlayer = (byte)dropdown.value;
        for (int x = 0; x < fogOfWarStatuses.GetLength(0); x++)
        {
            for (int y = 0; y < fogOfWarStatuses.GetLength(1); y++)
            {
                if (fogOfWarStatuses[x, y].viewType == nowPlayer + 1 || fogOfWarStatuses[x, y].viewType == 3)
                {
                    fogOfWarStatuses[x, y].status = FogOfWarViewStatus.Visited;
                    continue;
                }

                if (fogOfWarStatuses[x, y].viewType > 0 && nowPlayer == 2)
                {
                    fogOfWarStatuses[x, y].status = FogOfWarViewStatus.Visited;
                    continue;
                }
                fogOfWarStatuses[x, y].status = FogOfWarViewStatus.Hidden;
            }
        }

        
        switch (nowPlayer)
        {
            case 0:
                {
                    cam.cullingMask |= (1 << ChaserLayer);      // ChaserLayer 활성화
                    cam.cullingMask &= ~(1 << RunnerLayer);     // RunnerLayer 비활성화
                    Debug.Log("Chaser Layer 활성화");
                    break;
                }
            case 1:
                {
                    cam.cullingMask |= (1 << RunnerLayer);      // RunnerLayer 활성화
                    cam.cullingMask &= ~(1 << ChaserLayer);     // ChaserLayer 비활성화
                    Debug.Log("Runner Layer 활성화");
                    break;
                }
            default:
                {
                    cam.cullingMask |= (1 << RunnerLayer);      // RunnerLayer 활성화
                    cam.cullingMask |= (1 << ChaserLayer);      // ChaserLayer 활성화
                    Debug.Log("모든 레이어 활성화");
                    break;
                }
        }
    }

    #endregion

    public void CheckDistance()
    {
        if (nowPlayer >= 2)
        {
            return;
        }
        Vector2Int position = Characters[1 - nowPlayer].Astar.CurrentNode.Position;
        if (fogOfWarStatuses[position.x, position.y].status != FogOfWarViewStatus.Visible)
        {
            Characters[1 - nowPlayer].SetVisible();
        }
        else
        {
            Characters[1 - nowPlayer].gameObject.layer = VisibleLayer;
        }
    }
}


