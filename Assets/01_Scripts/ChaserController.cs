using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static Selector;

public class ChaserController : CharacterController //추격 그만두는 경우와 추격 위치 추측 알고리즘 추가할 것
{
    [SerializeField]
    private RaycastHit hit;
    private RaycastHit hitBottom;
    [SerializeField]
    protected RunnerController target; //타겟 캐릭터
    [SerializeField]
    private bool isChasing = false; // 추적 상태를 나타내는 변수
    [SerializeField]
    private bool isGuessed; // 타겟 위치 추정 여부
    private Vector3 targetDir;
    private Vector2Int targetDir2D;
    protected override void Update()
    {
        base.Update();
        
        
    }


    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.TryGetComponent<RunnerController>(out RunnerController runner))
        {
#if UNITY_EDITOR
            Debug.Log("추격 성공! 게임 종료");
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }


    public void Init(RunnerController _target)
    {
        target = _target;
        // 행동 트리 구성
        rootNode = new Selector(new List<Node>
        {
            // 도주 시퀀스
            new Sequence(new List<Node>
            {
                new CheckPlayerInSightNode(this, target, sightDistance),
                new ChaseNode(this, target, sightDistance, Chase)
            }),
            // 기본 행동: 순찰
           new PatrolNode(this, Move, SetDestination)
        });
        Astar.CurrentNode = Astar.TileDataList[Astar.StartPos.x, Astar.StartPos.y]; // 현재 타일 설정
        SetDestination();
    }

    private void Chase()
    {
        if (target == null)
        {
            isChasing = false; // 타겟이 없으면 추적 상태 해제
            return; //타겟이 없으면 종료
        }
        if(!isChasing)
        {
            AStarCount(); // A* 알고리즘을 사용하여 경로 계산
            isChasing = true; // 추적 상태로 변경
        }
        
        Move();
        if(Astar.CurrentNode == Astar.Path[Astar.Path.Count - 1])
        {
            AStarCount(); // 도착 노드에 도달하면 경로 재계산
        }

    }

    private void AStarCount()
    {
        if (isInSight)
        {
            isGuessed = false; // 타겟이 시야에 있으면 위치 추정 초기화
            Astar.Destination = target.Astar.CurrentNode.Position; //타겟의 현재 노드 위치를 도착지로 설정
        }
        else
        {
            GuessTargetPosition(); //타겟의 위치를 추정
        }

      
        Physics.Raycast(transform.position, -transform.up, out hitBottom, 10f);
        Vector2Int currentPosition = Astar.CurrentNode.Position;
        for (int i = 0; i < Astar.TileDataList.GetLength(0); i++)
        {
            for (int j = 0; j < Astar.TileDataList.GetLength(1); j++)
            {
                if (Tiles[i, j] == hitBottom.transform)
                {
                    currentPosition = new Vector2Int(i, j);
                    break;
                }
            }
        }
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
        Astar.StartPos = currentPosition; //시작 위치 업데이트
        Astar.OpenList.Add(Astar.TileDataList[currentPosition.x, currentPosition.y]); //열린 타일 리스트에 시작 위치 추가
        Astar.AStarAlgorithm();
    }


    private void GuessTargetPosition()
    {
        if (isGuessed)
        {
            return; // 이미 타겟 위치를 추정한 경우, 추가 추정은 하지 않음
        }
        isGuessed = true; // 타겟 위치 추정 상태로 변경

        targetDir = target.transform.forward; // 타겟의 이동 방향
        targetDir2D = new Vector2Int(
    (int)Mathf.Sign(targetDir.x),
    (int)Mathf.Sign(targetDir.z)
);// 2D 방향 벡터로 변환

        if (Astar.TileDataList[(Astar.Destination.x + targetDir2D.x * 2), (Astar.Destination.y + targetDir2D.y * 3)].IsBlock)
        {
            Astar.Destination = Astar.Destination + targetDir2D;
        }
        else
        {
            Astar.Destination = Astar.Destination + targetDir2D * 3; // 타겟의 현재 위치에 이동 방향을 더하여 추정 위치 설정
        }
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
    public void EndChase()
    {
        Debug.Log("End Chase"); // 추적 종료 로그
        Astar.Path.Clear(); // 경로 리스트 초기화
        isChasing = false; // 추적 상태 해제
        status = CharacterStatus.Moving; // 상태를 대기 상태로 설정
    }

    public void SetStatus()
    {
        
        status = CharacterStatus.Moving; // 상태를 이동으로 설정
    }
    public bool GetState()
    {
        return isChasing; // 현재 추적 상태 반환
    }
}