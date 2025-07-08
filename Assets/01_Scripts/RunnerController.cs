using System.Collections.Generic;
using UnityEngine;
using System.Collections;


using static Selector;
using UnityEditor.Experimental.GraphView;

public class RunnerController : CharacterController // 도주하는 방식에 오류가 있음. 확인할 것.
{
    [SerializeField]
    private RaycastHit hit;
    private RaycastHit hitBottom;
    protected float runSightDistance;
    [SerializeField]
    private bool firstRunTurn = false;
    [SerializeField]
    private bool isRunning = false; // 도주 상태를 나타내는 변수
    [SerializeField]
    private bool isInitialTurnStarted = false; // 첫 도주 회전이 시작되었는지 여부

    [SerializeField]
    private float runTime = 20f; // 도주 시간
    [SerializeField]
    protected ChaserController target; //타겟 캐릭터
    Vector3 direction = Vector3.zero;

    public bool IsRunning() { return isRunning; }


    public void Init(ChaserController _target, float viewAngle)
    {
        target = _target;
        runSightDistance = 1.5f;
        this.viewAngle = viewAngle;
        rootNode = new Selector(new List<Node>
        {
            new RunAwayNode(this, target, Run),

            new Sequence(new List<Node>
            {
                new CheckPlayerInSightNode(this, target, sightDistance),
                new ActionNode(() => { isRunning = true; Run(); })
            }),

            new PatrolNode(this, Move, SetDestination)
        });
        Astar.CurrentNode = Astar.TileDataList[Astar.StartPos.x, Astar.StartPos.y];
        SetDestination();
    }

    protected override void Update()
    {
        base.Update();
        if (isRunning && !isInSight)
        {
            RunTimeCheck(); // 도주 시간 체크
        }
    }
    private void Run()
    {
        isRunning = true;

        if (!firstRunTurn)
        {
            if (!firstRunTurn) // 이전에 추가했던 코루틴 가드 로직
            {
                status = CharacterStatus.Turning;
                StartRunningTurn(); // 첫 도주 회전 시작
            }
            return;
        }

        if (status == CharacterStatus.Turning)
        {
            targetPos = transform.position + direction * 10f;
            base.TurnTowards(targetPos);
            return;
        }
        if (status == CharacterStatus.Moving)
        {
            RunFront();
            return;
        }
    }

    public void ResetPath()
    {
        movementCount = 0; //이동 횟수 초기화
        Astar.Path.Clear(); //경로 리스트 초기화
        Astar.OpenList.Clear(); //열린 타일 리스트 초기화
        Astar.ClosedList.Clear(); //닫힌 타일 리스트 초기화
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
        
    }

    public void StartAstar()
    {
        Astar.StartPos = Astar.CurrentNode.Position; //시작 위치 업데이트
        Astar.OpenList.Add(Astar.CurrentNode); //열린 타일 리스트에 시작 위치 추가
        Astar.Destination = new Vector2Int(Random.Range(0, Tiling.Instance.Tiles.GetLength(0) - 1), Random.Range(0, Tiling.Instance.Tiles.GetLength(1) - 1));
        Astar.AStarAlgorithm(); //A* 알고리즘 실행
    }

 


    private void RunFront()
    {
        if (Physics.Raycast(transform.position, transform.forward, out hit, runSightDistance, 1 << 6))
        {

            float distanceL = float.MaxValue;
            float distanceR = float.MaxValue;
            if (Physics.Raycast(transform.position, -transform.right, out hit, 10, 1 << 6))
            {
                distanceL = hit.distance;
            }
            if (Physics.Raycast(transform.position, transform.right, out hit, 10, 1 << 6))
            {
                distanceR = hit.distance;
            }
            this.direction = (distanceL > distanceR) ? -transform.right : transform.right;

            status = CharacterStatus.Turning;
            return;
        }

        transform.position = Vector3.MoveTowards(transform.position, transform.position + transform.forward, 2f * Time.deltaTime);

        Transform currentTransform = Physics.Raycast(transform.position, -transform.up, out hitBottom, 10f) ? hitBottom.transform : null;
        Vector2Int currentPosition = Astar.CurrentNode.Position;
        for (int i = 0; i < Astar.TileDataList.GetLength(0); i++)
        {
            for (int j = 0; j < Astar.TileDataList.GetLength(1); j++)
            {
                if (Tiles[i, j] == currentTransform)
                {
                    currentPosition = new Vector2Int(i, j);
                    break;
                }
            }
        }
        Astar.CurrentNode = Astar.TileDataList[currentPosition.x, currentPosition.y];

        if (runTime <= 0f)
        {
            Debug.Log("도주 시간 종료");
            ResetRun();
            return;
        }
        firstRunTurn = true;
    }


    public override void HasLineOfSight()
    {
        base.HasLineOfSight();
        Vector3 startWorldPos = transform.position;
        Vector3 targetPos = target.transform.position;
        Vector3 targetDirection = (targetPos - startWorldPos).normalized;
        Vector3 direction = transform.forward;

        float dotProduct = Vector3.Dot(direction, targetDirection);
        float minDotProduct = Mathf.Cos(viewAngle * 0.5f * Mathf.Deg2Rad);
        if (dotProduct < minDotProduct)
        {
            isInSight = false; // 시야 밖
        }


        RaycastHit hit;
        if (Physics.Linecast(startWorldPos, targetPos, out hit, LayerMask.GetMask("Wall")))
        {
            isInSight = false; // 벽에 가려져 있으면 시야 밖
            return;
        }
        isInSight = true; // 벽에 가려지지 않으면 시야 안
    }

    private void StartRunningTurn()
    {
        if (!isInitialTurnStarted)
        {
            direction = -transform.forward;
            isInitialTurnStarted = true; 
        }
        if (runTime <= 0f)
        {
            Debug.Log("도주 시간 종료");
            ResetRun();
            return; 
        }
        if (Vector3.Angle(transform.forward, direction) <= 0.1f)
        {
            firstRunTurn = true;
            status = CharacterStatus.Moving; 
            Debug.Log("도주 회전 완료"); 
            return;
        }
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 360f * Time.deltaTime);        
    }

    public void ResetRun()
    {
        Debug.Log("도주 초기화");
        firstRunTurn = false;
        isRunning = false; // 도주 상태 초기화
        runTime = 20f; // 도주 시간 초기화
        ResetPath(); // A* 경로 초기화
    }
    public void RunTimeCheck()
    {
        runTime -= Time.deltaTime;
    }

    public float GetCurrentRunTime()
    {
        return runTime;
    }
}

 
