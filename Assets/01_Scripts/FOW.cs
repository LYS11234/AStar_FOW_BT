using System;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering;
using System.Linq;


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

    public FogOfWarStatus[,] fogOfWarStatuses = new FogOfWarStatus[0, 0];
    [SerializeField]
    private Texture2D fogTexture;
    public Material fogMaterial;
    [SerializeField]
    private Shader fowShader;


    public int tileWidthCount;
    public int tileHeightCount;

    private const int RunnerLayer = 9;
    private const int ChaserLayer = 8;
    private const int VisibleLayer = 10;

    public CharacterController[] Characters;
    private byte maxPlayers = 2;
    [SerializeField]
    private Transform[] characterTf;

    public float viewAngle;
    [SerializeField] private TMP_Dropdown dropdown;
    [SerializeField]
    private byte nowPlayer;
    [SerializeField]
    private Camera cam;

    [SerializeField]
    private List<Vector2Int>[] visitedList;
    // 각 플레이어가 방문한 타일 리스트
    [SerializeField]
    private List<Vector2Int>[] visibleLists;
    Color32[] colors;
    private const int MAX_REVEALERS = 100;

    public void Init(int _tileWidthCount, int _tileHeightCount)
    {
       
        gameManager = GameManager.Instance;
        characterTf = new Transform[maxPlayers];
        tileWidthCount = _tileWidthCount;
        tileHeightCount = _tileHeightCount;
        CreateFogTexture();
        cam = Camera.main;
        fogOfWarStatuses = new FogOfWarStatus[tileWidthCount, tileHeightCount];
        colors = new Color32[tileWidthCount * tileHeightCount];
        cam.depthTextureMode = DepthTextureMode.Depth;
        visitedList = new List<Vector2Int>[maxPlayers];
        visibleLists = new List<Vector2Int>[maxPlayers];
        for (int i = 0; i < maxPlayers; i++)
        {
            visitedList[i] = new List<Vector2Int>();
            visibleLists[i] = new List<Vector2Int>();
        }

        ResetFog();

        characterTf[0] = Characters[0].transform;
        characterTf[1] = Characters[1].transform;
        OnValueChange();
    }

    private void CreateFogTexture()
    {
        fogTexture = new Texture2D(tileWidthCount, tileHeightCount, TextureFormat.Alpha8, false);
        
        fogTexture.filterMode = FilterMode.Point;
        fogTexture.wrapMode = TextureWrapMode.Clamp;
        if(colors.IsUnityNull())
        {
            colors = new Color32[tileWidthCount * tileHeightCount];
        }
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = new Color32(0, 0, 0, 255);
        }
        
        if (fogMaterial != null)
        {
            fogMaterial.mainTexture = fogTexture;
            fogMaterial.mainTextureScale = new Vector2(-1, -1);
            fogMaterial.mainTextureOffset = new Vector2(1, 1);

        }
        fogTexture.SetPixels32(colors);
        fogTexture.Apply();
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

    public void OnNotify(byte eventType, object data0, object data1, object data2 = null)
    {
        
        switch (eventType)
        {
            case ConstDataType.fowSight:
                {
                    List<Vector2Int> tiles = data1 as List<Vector2Int>;
                    AddVisibleTiles(tiles, (byte)data0);

                    break;
                }
            default:
                {
                    break;
                }
        }
    }


    private void AddVisibleTiles(List<Vector2Int> tiles, byte layer)
    {
        visibleLists[layer - 8].Clear();
        for (int i = 0; i < tiles.Count; i++)
        {
            CheckVisibleTiles(tiles[i], layer);
        }
        visibleLists[layer - 8].AddRange(tiles);
        CheckVisiblity();
    }

    private void CheckVisibleTiles(Vector2Int tile, byte layer)
    {
        byte currenPlayer = (byte)(layer - 8);
        if (visitedList[currenPlayer].Contains(tile))
        {
            return;
        }
        visitedList[currenPlayer].Add(tile);
        
        
        
    }
    
    private void CheckVisiblity()
    {
        if(visitedList.IsUnityNull())
        {
            return;
        }
        switch (nowPlayer)
        {
            case 2:
                {
                    UpdateFogTexture();
                    break;
                }
            default:
                {
                    UpdateFogTexture(visitedList[nowPlayer]);
                    break;
                }
        }
        
    }

    private void UpdateFogTexture(List<Vector2Int> _visitedTileList = null)
    {
        List<Vector2Int> visitedTileList = _visitedTileList;
        List<Vector2Int> visibles = new List<Vector2Int>();
        switch(nowPlayer)
        {            
            case 0:
                {
                    visitedTileList = visitedList[0];
                    visibles = visibleLists[0];
                    break;
                }
            case 1:
                {
                    visitedTileList = visitedList[1];
                    visibles = visibleLists[1];
                    break;
                }
            default:
                {
                    visitedTileList = new List<Vector2Int>();
                    visitedTileList.AddRange(visitedList[0]);
                    visitedTileList.AddRange(visitedList[1]);
                    visibles.AddRange(visibleLists[0]);
                    visibles.AddRange(visibleLists[1]);
                    break;
                }
        }
        for(int i = 0; i < visitedTileList.Count; i++)
        {
            Vector2Int tile = visitedTileList[i];
            if (tile.x < 0 || tile.x >= tileWidthCount || tile.y < 0 || tile.y >= tileHeightCount)
            {
                continue;
            }
            int index = tile.y * tileWidthCount + tile.x; // 1D 인덱스로 변환
            colors[index] = new Color32(0, 0, 0, 254); // 검은색, 반투명 (회색 느낌)
        }
        
        for (int i = 0; i < visibles.Count; i++)
        {
            Vector2Int tile = visibles[i];
            if (tile.x < 0 || tile.x >= tileWidthCount || tile.y < 0 || tile.y >= tileHeightCount)
            {
                continue;
            }
            int index = tile.y * tileWidthCount + tile.x; // 1D 인덱스로 변환
            colors[index] = new Color32(0, 0, 0, 0); // 투명 (시야가 보이는 부분은 완전히 투명하게 처리)
        }
        fogTexture.SetPixels32(colors);
        fogTexture.Apply(); // 변경사항 GPU에 적용
        
    }
    #endregion





    #region Dropdown Event
    public void OnValueChange()
    {
        nowPlayer = (byte)dropdown.value;
        ResetFog();

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

    private void ResetFog()
    {
        var xRange = Enumerable.Range(0, tileWidthCount);
        var yRange = Enumerable.Range(0, tileHeightCount);
        var targetTiles = xRange.SelectMany(x => yRange.Select(y => new Vector2Int(x, y))); // 타겟 타일 리스트 생성

        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = new Color32(0, 0, 0, 255); // 초기화
        }
    }
}


