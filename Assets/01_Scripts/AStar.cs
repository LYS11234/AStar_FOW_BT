using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class AStar
{
    #region A* Algorithm Variables
    [Header("A* Algorithm Variables")]
    public PriorityQueue<TileData> OpenList = new(); //열린 타일 리스트
    public HashSet<TileData> ClosedList = new HashSet<TileData>(); //닫힌 타일 리스트
    public List<TileData> Path = new List<TileData>(); //경로 타일 리스트

    public TileData[,] TileDataList; //타일 데이터 리스트
    public TileData CurrentNode; //현재 노드
    public Vector2Int Destination; //목표 위치
    public Vector2Int StartPos;
    #endregion

    public CharacterStatus Status;

    public void AStarAlgorithm()
    {

        if (OpenList.Count == 0)
        {
            Debug.Log("No path found");
            return; //열린 타일이 없으면 경로를 찾을 수 없음
        }
        while (OpenList.Count > 0)
        {


            TileData currentNode = OpenList.Dequeue(); //선택한 노드를 열린 타일 리스트에서 제거
            if (ClosedList.Contains(currentNode))
            {
                continue; //현재 노드가 닫힌 타일 리스트에 있으면 무시
            }
            ClosedList.Add(currentNode); //현재 노드를 닫힌 타일 리스트에 추가
            if (currentNode.Position == Destination)
            {
                Path.Add(currentNode); //목표 위치에 도달하면 경로에 추가
                break; //경로 찾기 종료
            }
            if (currentNode.Position.x > 0)
            {
                CheckNode(currentNode.Position, new Vector2Int(-1, 0)); //왼쪽 타일 체크
            }
            if (currentNode.Position.x < TileDataList.GetLength(0) - 1)
            {
                CheckNode(currentNode.Position, new Vector2Int(1, 0)); //오른쪽 타일 체크
            }
            if (currentNode.Position.y > 0)
            {
                CheckNode(currentNode.Position, new Vector2Int(0, -1)); //아래 타일 체크
            }
            if (currentNode.Position.y < TileDataList.GetLength(1) - 1)
            {
                CheckNode(currentNode.Position, new Vector2Int(0, 1)); //위 타일 체크
            }

        }

        ConfirmPath(); //경로 확인 및 설정

    }

    private void CheckNode(Vector2Int position, Vector2Int checkPos)
    {
        if (TileDataList[position.x + checkPos.x, position.y + checkPos.y].IsBlock)
        {
            return; //이동 불가능한 타일이면 무시
        }
        if (ClosedList.Contains(TileDataList[position.x + checkPos.x, position.y + checkPos.y]))
        {
            return; //이미 닫힌 타일이면 무시
        }
        TileData newNode = TileDataList[position.x + checkPos.x, position.y + checkPos.y];
        newNode.GValue = TileDataList[position.x, position.y].GValue + 1; //G값 설정
        newNode.HValue = Mathf.Abs((Destination.x - newNode.Position.x)) + Mathf.Abs((Destination.y - newNode.Position.y));//H값 맨해튼 거리로 설정
        newNode.FValue = newNode.GValue + newNode.HValue; //F값 설정
        newNode.Parent = TileDataList[position.x, position.y]; //부모 노드 설정
        OpenList.Enqueue(newNode); //열린 타일 리스트에 추가
    }


    protected void ConfirmPath()
    {
        if (Path.Count == 0)
        {
            Debug.LogError("No path found");
            return; //경로가 없으면 종료
        }
        CurrentNode = Path[Path.Count - 1]; //경로의 마지막 노드
        Path.RemoveRange(0, Path.Count - 1); //경로 초기화
        Path.Add(CurrentNode); //현재 노드를 경로에 추가
        while (CurrentNode.Parent != null)
        {

            Path.Add(CurrentNode.Parent); //부모 노드를 경로에 추가
            CurrentNode = CurrentNode.Parent; //현재 노드를 부모 노드로 업데이트
        }

        Path.Reverse(); //경로를 역순으로 변경

    }

    public void FindPath()
    {

        ResetTiles(); //타일 초기화

        do
        {
            Destination = new Vector2Int(Random.Range(0, TileDataList.GetLength(0) - 1), Random.Range(0, TileDataList.GetLength(1) - 1));
        } while (TileDataList[Destination.x, Destination.y].IsBlock);
        OpenList.Enqueue(TileDataList[StartPos.x, StartPos.y]); //열린 타일 리스트에 시작 위치 추가
        AStarAlgorithm(); //A* 알고리즘 실행
    }
    public void ResetTiles()
    {
        OpenList.Clear(); //열린 타일 리스트 초기화
        ClosedList.Clear(); //닫힌 타일 리스트 초기화
        Path.Clear(); //경로 리스트 초기화
        StartPos = CurrentNode.Position; //시작 위치 설정
        var xRange = Enumerable.Range(0, TileDataList.GetLength(0));
        var yRange = Enumerable.Range(0, TileDataList.GetLength(1));
        var _tiles = xRange.SelectMany(x => yRange.Select(y => new Vector2Int(x, y))).ToArray(); // 타겟 타일 리스트 생성
        foreach (var tile in _tiles)
        {
            TileDataList[tile.x, tile.y].GValue = 0; //G값 초기화
            TileDataList[tile.x, tile.y].HValue = 0; //H값 초기화
            TileDataList[tile.x, tile.y].FValue = 0; //F값 초기화
            TileDataList[tile.x, tile.y].Parent = null; //부모 노드 초기화
        }
    }

    public void UpdateCurrentNode(Vector2Int position)
    {
        if (TileDataList[position.x, position.y].IsBlock)
        {
            Debug.LogError("Cannot update current node to a blocked tile");
            return; //이동 불가능한 타일이면 업데이트하지 않음
        }
        CurrentNode = TileDataList[position.x, position.y]; //현재 노드 업데이트
    }
}