using System.Collections.Generic;
using UnityEngine;
using System.Collections;


using static Selector;
using UnityEditor.Experimental.GraphView;
using Unity.VisualScripting;
using Unity.Android.Gradle.Manifest;

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

    [SerializeField]
    private bool isTurnning;

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
        HasLineOfSight(); // 타겟과의 시야 확인
        if (isRunning)
        {
            RunTimeCheck(); // 도주 시간 체크
            return;
        }
        
    }
    private void Run()
    {
        isRunning = true;
        velocity = 3f;
        if (!firstRunTurn)
        {
            status = CharacterStatus.Turning;
            StartRunningTurn(); // 첫 도주 회전 시작
            return;
        }

        if (status == CharacterStatus.Turning)
        {
            TurnTowards(targetPos); // 타겟 위치로 회전
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
        if (Physics.Raycast(transform.position, transform.forward, out hit, runSightDistance, 1 << 6) && !isTurnning)
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
            targetPos = SnapCardinal(direction);
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


    protected Vector3 SnapCardinal(Vector3 targetDirection)
    {
        targetDirection.y = 0;
        // y축 방향을 0으로 설정하여 수평 방향으로만 회전
        targetDirection.Normalize();
        Vector3 dir = Vector3.zero;
        if (Mathf.Abs(targetDirection.x) > Mathf.Abs(targetDirection.z))
        {
            dir = new Vector3(Mathf.Sign(targetDirection.x), 0, 0); // x축 방향으로 회전
        }
        else
        {
            dir = new Vector3(0, 0, Mathf.Sign(targetDirection.z)); // z축 방향으로 회전
        }

        return dir;
    }

    //protected void TurnTowards(Vector3 targetDirection)
    //{
    //    targetDirection.y = 0;
    //    if (Vector3.Angle(transform.forward, targetDirection) % 90f > 0)
    //    {
    //        float
    //        //if (transform.eulerAngles.y % 90 < 0.1f || transform.eulerAngles.y % 90 > 89.9f)
    //        //{
    //        //    float angle = 90 - transform.eulerAngles.y % 90 < 0.1f ? 0 : transform.eulerAngles.y % 90 - 90;
    //        //    Debug.Log($"회전 각도: {angle}");
    //        //    Debug.Log($"현재 회전 각도: {transform.eulerAngles.y}");
    //        //    transform.eulerAngles = new Vector3(0, transform.eulerAngles.y + angle);
    //        //    isTurnning = false; // 회전 완료
    //        //    status = CharacterStatus.Moving;
    //        //    return;
    //        //}
    //    }
    //    Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
    //    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 360f * Time.deltaTime);
    //}


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
            return;
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
        isActioning = true; // 행동 중 상태 설정
        if (!isInitialTurnStarted)
        {
            direction = SnapCardinal(-transform.forward); // 현재 방향을 90도로 스냅
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
        ResetPath(); // A* 경로 초기화
    }

    public void ResetRun()
    {
        Debug.Log("도주 초기화");
        isActioning = false; // 행동 중 상태 해제
        velocity = 2f; // 속도 초기화    
        isInitialTurnStarted = false; // 첫 도주 회전 상태 초기화
        firstRunTurn = false;
        isRunning = false; // 도주 상태 초기화
        isTurnning = false; // 회전 상태 초기화
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

 
