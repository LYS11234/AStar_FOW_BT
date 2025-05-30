using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System;
using Unity.VisualScripting;
using System.Linq;

using Random = UnityEngine.Random;
using Mono.Cecil;

public class CharacterController : MonoBehaviour
{
    #region A* Algorithm Variables
    [Header("A* Algorithm Variables")]

    [SerializeField]
    private List<TileData> openList = new List<TileData>(); //열린 타일 리스트
    [SerializeField]
    private List<TileData> closedList = new List<TileData>(); //닫힌 타일 리스트
    [SerializeField]
    private List<TileData> path = new List<TileData>(); //경로 타일 리스트

    public TileData[,] TileDatas; //타일 데이터 리스트

    [SerializeField]
    private Vector2Int destination; //목표 위치
    public Vector2Int StartPos;

    #endregion

    #region Character Movement Variables
    public bool IsStart;
    [SerializeField]
    private RaycastHit hit; //레이캐스트 히트 정보
    [SerializeField]
    private int movementCount = 0; //이동 카운트
    public Transform[,] Tiles; //타일 배열


    #endregion

    private void Update()
    {
        if (!IsStart)
        {
            return;
        }
        Move(); //캐릭터 이동
    }


    public virtual void SetDestination()
    {
        Random.InitState((int)DateTime.Now.Ticks); //랜덤 시드 초기화
        movementCount = 0; //이동 카운트 초기화
        destination = new Vector2Int(Random.Range(0, Tiling.Instance.Tiles.GetLength(0) - 1), Random.Range(0, Tiling.Instance.Tiles.GetLength(1) - 1));
        path.Clear(); //경로 리스트 초기화
        openList.Clear(); //열린 타일 리스트 초기화
        closedList.Clear(); //닫힌 타일 리스트 초기화
        for(int i = 0; i < TileDatas.GetLength(0); i++)
        {
            for (int j = 0; j < TileDatas.GetLength(1); j++)
            {
                TileDatas[i, j].GValue = 0; //G값 초기화
                TileDatas[i, j].HValue = 0; //H값 초기화
                TileDatas[i, j].FValue = 0; //F값 초기화
                TileDatas[i, j].Parent = null; //부모 노드 초기화
            }
        }

        while (TileDatas[destination.x, destination.y].IsBlock)
        {
            destination = new Vector2Int(Random.Range(0, Tiling.Instance.Tiles.GetLength(0) - 1), Random.Range(0, Tiling.Instance.Tiles.GetLength(1) - 1));
        }
        openList.Add(TileDatas[StartPos.x, StartPos.y]);
        IsStart = true; //경로 찾기 시작 플래그 설정
        AStarAlgorithm(); //경로 찾기 시작
    }


    protected virtual void AStarAlgorithm()
    {
        
        if (openList.Count == 0)
        {
            Debug.Log("No path found");
            return; //열린 타일이 없으면 경로를 찾을 수 없음
        }
        while (openList.Count > 0)
        {

            openList.Sort((a, b) => a.FValue.CompareTo(b.FValue)); //F값 기준으로 열린 타일 리스트 정렬
            TileData currentNode = openList[0]; //가장 F값이 작은 타일을 현재 노드로 설정
            openList.Remove(currentNode); //현재 노드를 열린 타일 리스트에서 제거
            closedList.Add(currentNode); //현재 노드를 닫힌 타일 리스트에 추가
            if (currentNode.Position == destination)
            {
                Debug.Log("Path found");
                path.Add(currentNode); //목표 위치에 도달하면 경로에 추가
                break ; //경로 찾기 종료
            }

            if (currentNode.Position.x > 0)
            {
                CheckNode(currentNode.Position, new Vector2Int(-1, 0)); //왼쪽 타일 체크
            }
            if (currentNode.Position.x < TileDatas.GetLength(0) - 1)
            {
                CheckNode(currentNode.Position, new Vector2Int(1, 0)); //오른쪽 타일 체크
            }
            if (currentNode.Position.y > 0)
            {
                CheckNode(currentNode.Position, new Vector2Int(0, -1)); //아래 타일 체크
            }
            if (currentNode.Position.y < TileDatas.GetLength(1) - 1)
            {
                CheckNode(currentNode.Position, new Vector2Int(0, 1)); //위 타일 체크
            }
        }

        ConfirmPath(); //경로 확인 및 설정
    }

    protected virtual void CheckNode(Vector2Int position, Vector2Int checkPos)
    {
        if (TileDatas[position.x + checkPos.x, position.y + checkPos.y].IsBlock)
        {
            return; //이동 불가능한 타일이면 무시
        }
        if (closedList.Contains(TileDatas[position.x + checkPos.x, position.y + checkPos.y]))
        {
            return; //이미 닫힌 타일이면 무시
        }
        if (openList.Contains(TileDatas[position.x + checkPos.x, position.y + checkPos.y]))
        {
            TileData existingNode = openList.Find(x => x.Position == TileDatas[position.x + checkPos.x, position.y + checkPos.y].Position);
            if (existingNode.GValue > TileDatas[position.x, position.y].GValue + 1)
            {
                existingNode.GValue = TileDatas[position.x, position.y].GValue + 1; //G값 업데이트
                existingNode.FValue = existingNode.GValue + existingNode.HValue; //F값 업데이트
                existingNode.Parent = TileDatas[position.x, position.y]; //부모 노드 업데이트
            }
        }
        else
        {
            TileData newNode = TileDatas[position.x + checkPos.x, position.y + checkPos.y];
            newNode.GValue = TileDatas[position.x, position.y].GValue + 1; //G값 설정
            newNode.HValue = Mathf.Abs((destination.x - newNode.Position.x)) + Mathf.Abs((destination.y - newNode.Position.y));//H값 맨해튼 거리로 설정
            newNode.FValue = newNode.GValue + newNode.HValue; //F값 설정
            newNode.Parent = TileDatas[position.x, position.y]; //부모 노드 설정
            openList.Add(newNode); //열린 타일 리스트에 추가
        }
    }


    protected virtual void ConfirmPath()
    {
        if(path.Count == 0)
        {
            Debug.LogError("No path found");
            return; //경로가 없으면 종료
        }
        TileData currentNode = path[path.Count - 1]; //경로의 마지막 노드
        path.RemoveRange(0, path.Count - 1); //경로 초기화
        path.Add(currentNode); //현재 노드를 경로에 추가
        while (currentNode.Parent != null)
        {
            
            path.Add(currentNode.Parent); //부모 노드를 경로에 추가
            currentNode = currentNode.Parent; //현재 노드를 부모 노드로 업데이트
        }
        
        path.Reverse(); //경로를 역순으로 변경
        foreach (TileData tile in path)
        {
            Debug.Log($"Path: {tile.Position}");
        }
    }


    protected virtual void Move()
    {
        if (path.Count <= 0)
        {
            return;
        }

        Physics.Raycast(transform.position, Vector3.down, out hit, 100f, 1 << 7);

        if (hit.transform.name == path.Last().Name || movementCount >= path.Count - 1)
        {
            IsStart = false; //목표 위치에 도달하면 경로 찾기 종료
            StartPos = path.Last().Position; //시작 위치 업데이트
            SetDestination();
            return; //목표 위치에 도달하면 새로운 목표 설정
        }
        //transform.forward = new Vector3(path[movementCount + 1].Position.x - path[movementCount].Position.x, 0, path[movementCount + 1].Position.y - path[movementCount].Position.y).normalized;
        try
        {
            if (path[movementCount].Name == hit.transform.name)
            {
                transform.position = (Tiles[path[movementCount].Position.x, path[movementCount].Position.y].position + new Vector3(0, 0.5f, 0));
                movementCount++;
            }
            Debug.Log($"Forward: {new Vector3(path[movementCount + 1].Position.x - path[movementCount].Position.x, 0, path[movementCount + 1].Position.y - path[movementCount].Position.y).normalized}");
            //transform.rotation = Quaternion.LookRotation(new Vector3(path[movementCount + 1].Position.x - path[movementCount].Position.x, 0, path[movementCount + 1].Position.y - path[movementCount].Position.y), transform.up);
            transform.position = Vector3.Lerp(transform.position, Tiles[path[movementCount].Position.x, path[movementCount].Position.y].position + new Vector3(0, 0.5f, 0), Time.deltaTime * 2f);



            //for (int i = 0; i < Tiles.GetLength(0); i++)
            //{
            //    for(int j = 0; j < Tiles.GetLength(1); j++)
            //    {

            //    }
            //}
            Debug.Log(hit.transform.name);
        }
        catch
        {
            transform.position = (Tiles[path[movementCount].Position.x, path[movementCount].Position.y].position + new Vector3(0, 0.5f, 0));
            movementCount++;
        }
        

    }
}
