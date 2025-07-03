using System.Collections.Generic;
using UnityEngine;
using System.Collections;


using static Selector;

public class RunnerController : CharacterController
{
    [SerializeField]
    private RaycastHit hit;
    private RaycastHit hitBottom;
    protected float runSightDistance;
    private bool coroutinRun;
    private bool firstRunTurn = false;
    [SerializeField]
    protected ChaserController target; //타겟 캐릭터
    public  void Init(ChaserController _target)
    {
        target = _target;
        runSightDistance = 1.5f;
        // 행동 트리 구성
        rootNode = new Selector(new List<Node>
        {
            // 도주 시퀀스
            new Sequence(new List<Node>
            {
                new CheckPlayerInSightNode(this, target, sightDistance),
                new RunAwayNode(this, target, sightDistance, Run)
            }),
            // 기본 행동: 순찰
            new PatrolNode(this, Move, SetDestination)
        });
        SetDestination();
    }

    protected override void Update()
    {
        base.Update();
        Debug.DrawRay(transform.position, transform.forward * runSightDistance, Color.red); // 레이캐스트 시각화
    }
    private void Run()
    {
        Astar.Destination = target.Astar.CurrentNode.Position; //타겟의 현재 노드 위치를 도착지로 설정

        Physics.Raycast(transform.position, -transform.up, out hitBottom, 10f);
        Vector2Int currentPosition = Astar.CurrentNode.Position;
        for (int i = 0; i < Astar.TileDataList.GetLength(0); i++)
        {
            for (int j = 0; j < Astar.TileDataList.GetLength(1); j++)
            {
                if (Tiles[i,j] == hitBottom.transform)
                {
                    currentPosition = new Vector2Int(i, j);
                    break;
                }
            }
        }
        ResetPath();
        
        if (!firstRunTurn)
        {
            StartCoroutine(RunTurnFirst());
        }
        if (Physics.Raycast(transform.position, transform.forward, out hit, runSightDistance, 6))
        {
            StartCoroutine(RunTurn());
            return;
        }
        RunFront();
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
        transform.position = Vector3.MoveTowards(transform.position, transform.position + transform.forward, 2f * Time.deltaTime);
        firstRunTurn = true;
    }

    private IEnumerator RunTurn()
    {
        if(coroutinRun)
            yield break;
        coroutinRun = true;
        float distanceL = float.MaxValue;
        float distanceR = float.MaxValue;
        if (Physics.Raycast(transform.position, -transform.right, out hit, 10, 6))
        {
            distanceL = hit.distance;
        }
        if (Physics.Raycast(transform.position, transform.right, out hit, 10, 6))
        {
            distanceR = hit.distance;
        }
        Vector3 direction = distanceR > distanceL ? transform.right : -transform.right;

        while (Vector3.Angle(transform.forward, direction) > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 360f * Time.deltaTime);
            yield return null;
        }
        coroutinRun = false;
    }

    private IEnumerator RunTurnFirst()
    {
        while (Vector3.Angle(transform.forward, -transform.forward) > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(-transform.forward);

            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 360f * Time.deltaTime);
            yield return null;
        }
        firstRunTurn = true;
    }    

    public void ResetRun()
    {
        firstRunTurn = false;
        coroutinRun = false;
    }
}
 
