using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static Selector;

public class ChaserController : CharacterController
{
    [SerializeField]
    private RaycastHit hit;
    private RaycastHit hitBottom;
    [SerializeField]
    protected RunnerController target; //타겟 캐릭터

    protected override void Update()
    {
        base.Update();
        
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
        SetDestination();
    }

    private void Chase()
    {
        if (target == null)
        {
            return; //타겟이 없으면 종료
        }
        Astar.Destination = target.Astar.CurrentNode.Position; //타겟의 현재 노드 위치를 도착지로 설정
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
        Move();


    }

    public void SetStatus()
    {
        status = CharacterStatus.Moving; // 상태를 이동으로 설정
    }
}