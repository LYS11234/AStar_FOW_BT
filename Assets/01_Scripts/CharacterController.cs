using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

using Random = UnityEngine.Random;
using static UnityEngine.GraphicsBuffer;
using Unity.VisualScripting;
using UnityEngine.UIElements;






public enum CharacterStatus
{
    FindingPath = 0,
    Moving,
    Turning
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
    [SerializeField]
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
    protected Vector2Int revealerPosition = new Vector2Int(); //시야 위치 배열
    [SerializeField]
    protected float revealerRad = 0; //시야 반지름 배열

    protected Vector3 forward; //앞 방향 벡터
    [SerializeField]    
    protected List<Vector2Int> visibleTiles = new List<Vector2Int>(); //시야에 보이는 타일 리스트
    #endregion


    #region Debug
    public List<Vector2Int> path = new List<Vector2Int>(); //디버그용 경로 리스트
    #endregion

    protected void GetStart()
    {
        if (gameManager.IsUnityNull())
        {
            gameManager = GameManager.Instance;

            Astar.Status = status;
            fogOfWar = gameManager.fow; //FOW 인스턴스 가져오기
            
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
        Astar.FindPath(); //A* 알고리즘을 사용하여 경로 찾기
        isStart = true; //경로 찾기 시작 플래그 설정
        status = CharacterStatus.Moving;
        path = Astar.Path.Select(node => node.Position).ToList(); //경로 리스트 업데이트

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
        visibleTiles.Clear(); //시야에 보이는 타일 리스트 초기화
        revealerPosition = Astar.CurrentNode.Position; //현재 노드 위치 설정
        forward = transform.forward; //앞 방향 벡터 설정
        var xRange = Enumerable.Range(revealerPosition.x - sightDistance, 2 * sightDistance + 1); // 
        var yRange = Enumerable.Range(revealerPosition.y - sightDistance, 2 * sightDistance + 1);
        var targetTiles = xRange.SelectMany(x => yRange.Select(y => new Vector2Int(x, y))); // 타겟 타일 리스트 생성
        foreach (var targetTilePos in targetTiles)
        {
            CheckInSight(targetTilePos); // 시야에 보이는 타일 체크
        }
        NotifyObservers(ConstDataType.fowSight, layer, visibleTiles); //시야 업데이트 알림
    }

    private void CheckInSight(Vector2Int endTile)
    {
        
        Vector3 start = GetWorldPositionFromTile(revealerPosition) + Vector3.up * 0.5f;
        Vector3 end = GetWorldPositionFromTile(endTile) + Vector3.up * 0.5f;
        Vector3 dir = (end - start).normalized; // 
        float dotProduct = Vector3.Dot(forward, dir); //앞 방향과 현재 방향의 내적 계산
        float minDotProduct = Mathf.Cos(viewAngle * 0.5f * Mathf.Deg2Rad); //시야각의 코사인 값 계산
        if (dotProduct < minDotProduct)
        {
            return;
        }
        RaycastHit hit;
        if (Physics.Linecast(start, end, out hit, LayerMask.GetMask("Wall")))
        {
            return;
        }
        visibleTiles.Add(endTile); //시야에 보이는 타일 리스트에 추가
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
        
        targetPos = Tiles[Astar.Path[movementCount].Position.x, Astar.Path[movementCount].Position.y].position +
                   new Vector3(0, 0.5f, 0);
        if (movementCount < 1)
        {
            return; //첫 번째 이동은 타겟 위치 설정을 하지 않음
        }
       
        if (movementCount >= Astar.Path.Count)
        {
            return; //경로의 끝에 도달하면 타겟 위치 설정을 하지 않음

        }
        Astar.CurrentNode = Astar.Path[movementCount - 1]; //현재 노드 업데이트


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

    public void NotifyObservers(byte eventType, object data0, object data1, object data2 = null)
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
