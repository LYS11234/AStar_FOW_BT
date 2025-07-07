using UnityEngine;

public class Test : MonoBehaviour
{
    void Start()
    {
        // Tiling 스크립트가 타일을 모두 생성할 때까지 잠시 대기합니다.
        StartCoroutine(CheckTilePositions());
    }

    private System.Collections.IEnumerator CheckTilePositions()
    {
        // Tiling 스크립트의 Start 함수가 실행된 후를 보장하기 위해 한 프레임 대기
        yield return null;

        if (Tiling.Instance == null || Tiling.Instance.Tiles == null)
        {
            Debug.LogError("Tiling 인스턴스 또는 Tiles 배열을 찾을 수 없습니다.");
            yield break;
        }

        // 비교를 위해 두 개의 세로 타일 위치를 가져옵니다.
        Transform tile1 = Tiling.Instance.Tiles[0, 0];
        Transform tile2 = Tiling.Instance.Tiles[0, 1];

        Debug.Log("======== 타일 위치 디버그 결과 ========");
        Debug.Log($"Tile (0,0)의 월드 좌표: {tile1.position}");
        Debug.Log($"Tile (0,1)의 월드 좌표: {tile2.position}");
        Debug.Log($"두 타일의 Z축 거리 차이: {Mathf.Abs(tile1.position.z - tile2.position.z)}");
        Debug.Log("======================================");
    }
}