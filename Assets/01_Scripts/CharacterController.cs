using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

using Random = UnityEngine.Random;


public class AStar
{
    #region A* Algorithm Variables
    [Header("A* Algorithm Variables")]
    public List<TileData> OpenList = new List<TileData>(); //열린 타일 리스트
    public HashSet<TileData> ClosedList = new HashSet<TileData>(); //닫힌 타일 리스트
    public List<TileData> Path = new List<TileData>(); //경로 타일 리스트

    public TileData[,] TileDataList; //타일 데이터 리스트
    public Vector2Int Destination; //목표 위치
    public Vector2Int StartPos;
    #endregion
    
    public void AStarAlgorithm()
    {
        
        if (OpenList.Count == 0)
        {
            Debug.Log("No path found");
            return; //열린 타일이 없으면 경로를 찾을 수 없음
        }
        OpenList.Sort((a, b) => a.FValue.CompareTo(b.FValue)); //F값 기준으로 열린 타일 리스트 정렬
        while (OpenList.Count > 0)
        {

            
            TileData currentNode = OpenList[0]; //가장 F값이 작은 타일을 현재 노드로 설정
            OpenList.Remove(currentNode); //현재 노드를 열린 타일 리스트에서 제거
            ClosedList.Add(currentNode); //현재 노드를 닫힌 타일 리스트에 추가
            if (currentNode.Position == Destination)
            {
                Path.Add(currentNode); //목표 위치에 도달하면 경로에 추가
                break ; //경로 찾기 종료
            }
            OpenList.Sort((a, b) => a.FValue.CompareTo(b.FValue)); //F값 기준으로 열린 타일 리스트 정렬
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
        if (OpenList.Contains(TileDataList[position.x + checkPos.x, position.y + checkPos.y]))
        {
            TileData existingNode = OpenList.Find(x => x.Position == TileDataList[position.x + checkPos.x, position.y + checkPos.y].Position);
            if (existingNode.GValue > TileDataList[position.x, position.y].GValue + 1)
            {
                existingNode.GValue = TileDataList[position.x, position.y].GValue + 1; //G값 업데이트
                existingNode.FValue = existingNode.GValue + existingNode.HValue; //F값 업데이트
                existingNode.Parent = TileDataList[position.x, position.y]; //부모 노드 업데이트
            }
        }
        else
        {
            TileData newNode = TileDataList[position.x + checkPos.x, position.y + checkPos.y];
            newNode.GValue = TileDataList[position.x, position.y].GValue + 1; //G값 설정
            newNode.HValue = Mathf.Abs((Destination.x - newNode.Position.x)) + Mathf.Abs((Destination.y - newNode.Position.y));//H값 맨해튼 거리로 설정
            newNode.FValue = newNode.GValue + newNode.HValue; //F값 설정
            newNode.Parent = TileDataList[position.x, position.y]; //부모 노드 설정
            OpenList.Add(newNode); //열린 타일 리스트에 추가
        }
    }


    protected void ConfirmPath()
    {
        if(Path.Count == 0)
        {
            Debug.LogError("No path found");
            return; //경로가 없으면 종료
        }
        TileData currentNode = Path[Path.Count - 1]; //경로의 마지막 노드
        Path.RemoveRange(0, Path.Count - 1); //경로 초기화
        Path.Add(currentNode); //현재 노드를 경로에 추가
        while (currentNode.Parent != null)
        {
            
            Path.Add(currentNode.Parent); //부모 노드를 경로에 추가
            currentNode = currentNode.Parent; //현재 노드를 부모 노드로 업데이트
        }
        
        Path.Reverse(); //경로를 역순으로 변경
    }

}

public class CharacterController : MonoBehaviour
{
    #region A* Algorithm Variables
    [Header("A* Algorithm Variables")] 
    public readonly AStar Astar = new AStar();
    

    #endregion

    #region Character Movement Variables
    public bool IsStart;
    private RaycastHit hit; //레이캐스트 히트 정보
    [SerializeField]
    private int movementCount = 0; //이동 카운트
    public Transform[,] Tiles; //타일 배열
    [SerializeField]
    private float sightDistance;

    private Vector3 destination;
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
        Astar.Destination = new Vector2Int(Random.Range(0, Tiling.Instance.Tiles.GetLength(0) - 1), Random.Range(0, Tiling.Instance.Tiles.GetLength(1) - 1));
        Astar.Path.Clear(); //경로 리스트 초기화
        Astar.OpenList.Clear(); //열린 타일 리스트 초기화
        Astar.ClosedList.Clear(); //닫힌 타일 리스트 초기화
        destination = Tiles[Astar.Destination.x, Astar.Destination.y].position;
        for(int i = 0; i < Astar.TileDataList.GetLength(0); i++)
        {
            for (int j = 0; j < Astar.TileDataList.GetLength(1); j++)
            {
                Astar.TileDataList[i, j].GValue = 0; //G값 초기화
                Astar.TileDataList[i, j].HValue = 0; //H값 초기화
                Astar.TileDataList[i, j].FValue = 0; //F값 초기화
                Astar.TileDataList[i, j].Parent = null; //부모 노드 초기화
            }
        }

        while (Astar.TileDataList[Astar.Destination.x, Astar.Destination.y].IsBlock)
        {
            Astar.Destination = new Vector2Int(Random.Range(0, Tiling.Instance.Tiles.GetLength(0) - 1), Random.Range(0, Tiling.Instance.Tiles.GetLength(1) - 1));
        }
        Astar.OpenList.Add(Astar.TileDataList[Astar.StartPos.x, Astar.StartPos.y]);
        IsStart = true; //경로 찾기 시작 플래그 설정
        Astar.AStarAlgorithm(); //경로 찾기 시작
    }


    protected virtual void Move()
    {
        if (Astar.Path.Count <= 0)
        {
            return;
        }

        
        Vector3 targetPos = Tiles[Astar.Path[movementCount].Position.x, Astar.Path[movementCount].Position.y].position +
                            new Vector3(0, 0.5f, 0);
        if (Vector3.Distance(transform.position, destination) <= 0.01f || movementCount >= Astar.Path.Count - 1)
        {
            IsStart = false; //목표 위치에 도달하면 경로 찾기 종료
            Astar.StartPos = Astar.Path.Last().Position; //시작 위치 업데이트
            SetDestination();
            return; //목표 위치에 도달하면 새로운 목표 설정
        }

        Vector3 directionToTarget = targetPos - transform.position;
        directionToTarget.y = 0;
  
        if (Vector3.Angle(transform.forward, targetPos - transform.position) > 0.1f && directionToTarget != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
            
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 360f * Time.deltaTime);
            return; 
        }
        if (Vector3.Distance(transform.position, targetPos) <= 0.1f)
        {
            transform.position = targetPos;
            movementCount++;
            targetPos = Tiles[Astar.Path[movementCount].Position.x, Astar.Path[movementCount].Position.y].position +
                                new Vector3(0, 0.5f, 0);
            
            return;
        }
        
        transform.position = Vector3.MoveTowards(transform.position,
            Tiles[Astar.Path[movementCount].Position.x, Astar.Path[movementCount].Position.y].position + new Vector3(0, 0.5f, 0),
            Time.deltaTime * 2f);
    }
}
