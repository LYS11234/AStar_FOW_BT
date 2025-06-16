using UnityEngine;


public enum FogOfWarStatus
{
    Hidden = 0,
    Visited,
    Visible
}

public class FOW : MonoBehaviour
{
    private FogOfWarStatus[,] FogOfWarStatuses;
    private Texture2D fogTexture;
    public Material fogMaterial; 
    public int tileWidthCount;
    public int tileHeightCount;
    
    public void Init(int _tileWidthCount, int _tileHeightCount)
    {
        tileWidthCount = _tileWidthCount;
        tileHeightCount = _tileHeightCount;
        CreateFogTexture();
        
        FogOfWarStatuses = new FogOfWarStatus[tileWidthCount, tileHeightCount];

        for (var i = 0; i < tileHeightCount; i++)
        {
            for (var j = 0; j < tileWidthCount; j++)
            {
                FogOfWarStatuses[j, i] = FogOfWarStatus.Hidden;
            }
        }
        
        UpdateFogTexture();
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
        }
    }
    
    private void UpdateFogTexture()
    {
        Color32[] colors = new Color32[tileWidthCount * tileHeightCount];
        for (int y = 0; y < tileHeightCount; y++)
        {
            for (int x = 0; x < tileWidthCount; x++)
            {
                int index = y * tileWidthCount + x; // 1D 인덱스로 변환
                switch (FogOfWarStatuses[x, y])
                {
                    case FogOfWarStatus.Hidden:
                        colors[index] = new Color32(0, 0, 0, 255); // 검은색, 완전 불투명
                        break;
                    case FogOfWarStatus.Visited:
                        colors[index] = new Color32(0, 0, 0, 128); // 검은색, 반투명 (회색 느낌)
                        break;
                    case FogOfWarStatus.Visible:
                        colors[index] = new Color32(0, 0, 0, 0);   // 완전 투명
                        break;
                }
            }
        }
        fogTexture.SetPixels32(colors);
        fogTexture.Apply(); // 변경사항 GPU에 적용
    }

    public void UpdateFogOfWarStatus(Vector2Int position, float sightRadius)
    {
        
    }

}
