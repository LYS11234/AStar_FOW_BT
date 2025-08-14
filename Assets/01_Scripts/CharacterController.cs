using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

using Random = UnityEngine.Random;
using static UnityEngine.GraphicsBuffer;
using Unity.VisualScripting;


public class PriorityQueue<T> where T : IComparable<T>
{
    private T[] data;
    public int Count { get; private set; }
    public int MaxCount { get; private set; }

    public PriorityQueue()
    {
        MaxCount = 1;
        
        Count = 0;
        data = new T[MaxCount];
    }
    public PriorityQueue(int maxCount = int.MaxValue)
    {
        MaxCount = maxCount;
        Count = 0;
        data = new T[MaxCount];
    }

    private void Expand()
    {
        MaxCount *= 2;
        T[] newData = new T[MaxCount];
        Array.Copy(data, newData, Count);
        data = newData;
    }

    private void Swap(ref T left, ref T right)
    {
        T temp = left;
        left = right;
        right = temp;
    }

    public void Enqueue(T item)
    {
        if (Count == MaxCount)
        {
            Expand();
        }
        data[Count] = item; // 새 아이템을 배열의 끝에 추가
        Count++;
        
        int index = Count - 1;
        while (index > 0)
        {
            int parentIndex = (index - 1) / 2;
            if (data[index].CompareTo(data[parentIndex]) > 0)
            {
                break; // 부모 노드가 더 크면 종료
            }
            Swap(ref data[index], ref data[parentIndex]);
            index = parentIndex;
        }
    }
    public T Dequeue() // 큐에서 가장 작은 요소를 제거하고 반환합니다.
    {
        if (Count == 0)
        {
            throw new InvalidOperationException("Queue is empty");
        }
        T result = data[0];
        Count--;
        data[0] = data[Count];
        data[Count] = default(T); // 마지막 요소를 기본값으로 설정
        int index = 0;
        while (index < Count)
        {
            int leftChildIndex = index * 2 + 1;
            int rightChildIndex = index * 2 + 2;
            
            int next = index; // 현재 노드 인덱스
            if (leftChildIndex < Count && data[next].CompareTo(data[leftChildIndex]) > 0)
            {
                next = leftChildIndex; // 왼쪽 자식이 더 크면 왼쪽 자식 인덱스로 설정
            }
            if (rightChildIndex < Count && data[next].CompareTo(data[rightChildIndex]) > 0)
            {
                next = rightChildIndex; // 오른쪽 자식이 더 크면 오른쪽 자식 인덱스로 설정
            }
            if (index == next)
            {
                break; // 자식 노드가 없으면 종료
            }
            Swap(ref data[index], ref data[next]);
            index = next;
        }
        return result;
    }

    public T Peek()
    {
        if (Count == 0)
        {
            throw new InvalidOperationException("Queue is empty");
        }
        return data[0];
    }

   

    public void Clear()
    {
        Count = 0; // 큐를 비우기 위해 Count를 0으로 설정
        Array.Clear(data, 0, data.Length); // 데이터 배열을 초기화
    }
}



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

}

public class CharacterController : MonoBehaviour, ISubject
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
    protected float velocity = 2f;
    [SerializeField]
    protected bool isActioning = false; //행동 중인지 여부
    #endregion

    #region AI
    [SerializeField]
    protected Node rootNode; //AI 루트 노드


    #endregion
    [SerializeField]
    protected byte layer;

    public List<IObserver> Observer { get; protected set; } = new List<IObserver>();//옵저버 인터페이스
    protected GameManager gameManager;

    #region Sight
    protected Vector4 revealerPosition = new Vector4(); //시야 위치 배열
    [SerializeField]
    protected float revealerRad = 0; //시야 반지름 배열

    [SerializeField]
    protected Camera depthCamera; //뎁스 카메라
    [SerializeField]
    protected RenderTexture depthTexture; //뎁스 텍스처
    #endregion

    protected void GetStart()
    {
        if (gameManager.IsUnityNull())
        {
            gameManager = GameManager.Instance;

            Astar.Status = status;
            fogOfWar = gameManager.fow; //FOW 인스턴스 가져오기
            
            depthTexture = new RenderTexture(ConstVariables.Resolution, ConstVariables.Resolution, 24, RenderTextureFormat.Depth); //뎁스 텍스처 생성
            depthCamera.targetTexture = depthTexture; //뎁스 카메라의 타겟 텍스처 설정
        }
    }
    protected virtual void Update()
    {
        rootNode.Evaluate();
    }



    protected void SetDestination()
    {
       
        Random.InitState((int)DateTime.Now.Ticks); //랜덤 시드 초기화
        movementCount = 0; //이동 카운트 초기화
        Astar.Destination = new Vector2Int(Random.Range(0, gameManager.tiling.Tiles.GetLength(0) - 1), Random.Range(0, gameManager.tiling.Tiles.GetLength(1) - 1));
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
            Astar.Destination = new Vector2Int(Random.Range(0, gameManager.tiling.Tiles.GetLength(0) - 1), Random.Range(0, gameManager.tiling.Tiles.GetLength(1) - 1));
        }
        Astar.OpenList.Enqueue(Astar.TileDataList[Astar.StartPos.x, Astar.StartPos.y]);
        isStart = true; //경로 찾기 시작 플래그 설정
        Astar.AStarAlgorithm(); //경로 찾기 시작
        status = CharacterStatus.Moving;
    }



    protected void Move()
    {
        if(isActioning)
        {
            return; //행동 중이면 이동하지 않음
        }
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
        revealerPosition = transform.position; //시야 위치 설정
        revealerRad = sightDistance; //시야 반지름 설정

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

        if (Vector3.Distance(transform.position, targetPos) > 0.1f)
        {
            isMoving = true;
            transform.position = Vector3.MoveTowards(transform.position,
                Tiles[Astar.Path[movementCount].Position.x, Astar.Path[movementCount].Position.y].position + new Vector3(0, 0.5f, 0),
                Time.deltaTime * velocity);
            return;
        }
        
        transform.position = targetPos;
        if (movementCount >= 1 && movementCount < Astar.Path.Count)
        {
            Astar.CurrentNode = Astar.Path[movementCount - 1]; //현재 노드 업데이트
        }



        movementCount++;
        SetTargetPos();
        isMoving = false;

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

        NotifyObservers(ConstDataType.fowSight, layer, depthTexture);
        
    }

    protected void TurnTowards(Vector3 targetDirection)
    {
        // y축은 무시하여 수평 회전만 하도록 보장
        targetDirection.y = 0;

        if (Vector3.Angle(transform.forward, targetDirection) < 0.1f || targetDirection == Vector3.zero)
        {
            status = CharacterStatus.Moving;
            return;
        }
        Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 360f * Time.deltaTime);
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


    public void SetVisible()
    {
        gameObject.layer = layer; // 캐릭터 레이어 설정
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

    public void RegisterObserver(IObserver observer)
    {
        if(Observer.Contains(observer))
        {
            return; //이미 등록된 옵저버는 추가하지 않음
        }
        Observer.Add(observer); //옵저버 리스트에 추가
    }
    public void UnregisterObserver(IObserver observer)
    {
        Observer.Remove(observer); //옵저버 리스트에서 제거
    }
    public void NotifyObservers(byte eventType, object data)
    {

    }

    public void NotifyObservers(byte eventType, object data0, object data1)
    {
        switch (eventType)
        {
            case ConstDataType.hasLineOfSight:
                {
                    Observer[ConstTypes.FOW].OnNotify(eventType, data0, data1);
                    break;
                }
            case ConstDataType.action:
                {
                    break;
                }
            case ConstDataType.fow:
                {
                    
                    break;
                }
            case ConstDataType.fowSight:
                {
                    Observer[ConstTypes.FOW].OnNotify(eventType, data0, data1);
                    break;
                }
            default:
                {
                    
                    break;
                }
        }
    }

}