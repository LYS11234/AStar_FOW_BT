using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

using Random = UnityEngine.Random;
using static UnityEngine.GraphicsBuffer;





public enum CharacterStatus
{
    FindingPath = 0,
    Moving,
    Turning
}

public class AStar
{
    #region A* Algorithm Variables
    [Header("A* Algorithm Variables")]
    public List<TileData> OpenList = new List<TileData>(); //열린 타일 리스트
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


            TileData currentNode = OpenList[0]; //가장 F값이 작은 타일을 현재 노드로 설정
            OpenList.Remove(currentNode); //현재 노드를 열린 타일 리스트에서 제거
            ClosedList.Add(currentNode); //현재 노드를 닫힌 타일 리스트에 추가
            if (currentNode.Position == Destination)
            {
                Path.Add(currentNode); //목표 위치에 도달하면 경로에 추가
                break; //경로 찾기 종료
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

}

public class CharacterController : MonoBehaviour
{
    #region A* Algorithm Variables
    [Header("A* Algorithm Variables")]
    public readonly AStar Astar = new AStar();

    #endregion

    #region Character Movement Variables
    private bool isStart;
    private RaycastHit hit; //레이캐스트 히트 정보
    [SerializeField]
    protected int movementCount = 0; //이동 카운트
    public Transform[,] Tiles; //타일 배열
    [SerializeField]
    protected int sightDistance = 5;
    protected float viewAngle; //시야각
    private FOW fogOfWar;
    private Vector3 destination;
    [SerializeField]
    protected CharacterStatus status;
    [SerializeField]
    protected Vector3 targetPos;
    [SerializeField]
    protected bool isInSight;
    private bool isMoving;
    #endregion

    #region AI
    [SerializeField]
    protected Node rootNode; //AI 루트 노드


    #endregion
    private void Start()
    {
        Astar.Status = status;
        fogOfWar = FOW.Instance;
    }
    protected virtual void Update()
    {
        if (!isStart)
        {
            return;
        }

        rootNode.Evaluate();
    }



    protected void SetDestination()
    {
        
        
        Random.InitState((int)DateTime.Now.Ticks); //랜덤 시드 초기화
        movementCount = 0; //이동 카운트 초기화
        Astar.Destination = new Vector2Int(Random.Range(0, Tiling.Instance.Tiles.GetLength(0) - 1), Random.Range(0, Tiling.Instance.Tiles.GetLength(1) - 1));
        Astar.Path.Clear(); //경로 리스트 초기화
        Astar.OpenList.Clear(); //열린 타일 리스트 초기화
        Astar.ClosedList.Clear(); //닫힌 타일 리스트 초기화
        destination = Tiles[Astar.Destination.x, Astar.Destination.y].position;
        for (int i = 0; i < Astar.TileDataList.GetLength(0); i++)
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
        isStart = true; //경로 찾기 시작 플래그 설정
        Astar.AStarAlgorithm(); //경로 찾기 시작
        status = CharacterStatus.Moving;
    }



    protected void Move()
    {
        if (status == CharacterStatus.Moving)
        {
            MoveFront();
        }

        if (status == CharacterStatus.Turning)
        {
            Turn();
        }
    }

    public virtual void HasLineOfSight()
    {
        

    }
    public CharacterStatus GetStatus()
    {
        return status;
    }

    protected virtual void MoveFront()
    {

        if (Astar.Path.Count <= 0)
        {
            return;
        }



        if (Vector3.Distance(transform.position, destination) <= 0.01f || movementCount >= Astar.Path.Count - 1)
        {
            Astar.StartPos = Astar.Path.Last().Position; //시작 위치 업데이트
            Astar.Path.Clear(); //경로 리스트 초기화
            return; //목표 위치에 도달하면 새로운 목표 설정
        }
        if (Vector3.Angle(transform.forward, targetPos - transform.position) > 0.1f)
        {
            status = CharacterStatus.Turning;
            //return;
        }

        if (Vector3.Distance(transform.position, targetPos) <= 0.1f)
        {
            transform.position = targetPos;
            if (movementCount >= 1 && movementCount < Astar.Path.Count)
            {
                Astar.CurrentNode = Astar.Path[movementCount - 1]; //현재 노드 업데이트
            }
            


            movementCount++;
            SetTargetPos();
            isMoving = false;
            return;
        }
        isMoving = true;
        transform.position = Vector3.MoveTowards(transform.position,
            Tiles[Astar.Path[movementCount].Position.x, Astar.Path[movementCount].Position.y].position + new Vector3(0, 0.5f, 0),
            Time.deltaTime * 2f);


    }

    private void SetTargetPos()
    {
        if(movementCount < Astar.Path.Count)
        {
            
            targetPos = Tiles[Astar.Path[movementCount].Position.x, Astar.Path[movementCount].Position.y].position +
                    new Vector3(0, 0.5f, 0);
            if (movementCount >= 1)
            {
                Astar.CurrentNode = Astar.Path[movementCount - 1]; //현재 노드 업데이트
            }
        }
        
        
        
    }

    protected void TurnTowards(Vector3 targetDirection)
    {
        // y축은 무시하여 수평 회전만 하도록 보장
        targetDirection.y = 0;

        if (Vector3.Angle(transform.forward, targetDirection) > 0.1f && targetDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 360f * Time.deltaTime);
        }
        else // 회전이 완료되면 이동 상태로 변경
        {
            status = CharacterStatus.Moving;
        }
    }

    // ✨ 기존 Turn() 메소드는 내부 로직을 수정
    protected void Turn()
    {
        // 순찰 시에는 스스로 목적지를 설정하고
        SetTargetPos();
        Vector3 directionToTarget = targetPos - transform.position;

        // 새로 만든 TurnTowards를 호출하여 회전 실행
        TurnTowards(directionToTarget);
    }

    public int GetMovementCount()
    {
        return movementCount;
    }

    public int GetSightDistance()
    {
        return sightDistance;
    }

    public bool IsMoving()
    {
        return isMoving;
    }
    public bool GetInSight()
    {
        return isInSight;
    }
}