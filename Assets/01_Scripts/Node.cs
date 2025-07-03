using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

using Action = System.Action;
// Node.cs
public enum NodeState
{
    Running,
    Success,
    Failure
}

public abstract class Node
{
    // 각 노드는 이 Evaluate 메소드를 구현해야 함
    public abstract NodeState Evaluate();
}

public class Sequence : Node
{
    protected List<Node> children = new List<Node>();

    public Sequence(List<Node> children)
    {
        this.children = children;
    }

    public override NodeState Evaluate()
    {
        foreach (var node in children)
        {
            switch (node.Evaluate())
            {
                case NodeState.Failure:
                    // 하나라도 실패하면 즉시 실패 반환
                    return NodeState.Failure;
                case NodeState.Success:
                    // 성공하면 다음 노드로 계속
                    continue;
                case NodeState.Running:
                    // 하나라도 실행 중이면 전체가 실행 중
                    return NodeState.Running;
            }
        }
        // 모든 자식이 성공했을 때만 성공 반환
        return NodeState.Success;
    }
    
}

public class Selector : Node
{
    protected List<Node> children = new List<Node>();

    public Selector(List<Node> children)
    {
        this.children = children;
    }

    public override NodeState Evaluate()
    {
        foreach (var node in children)
        {
            switch (node.Evaluate())
            {
                case NodeState.Failure:
                    // 실패하면 다음 노드로 계속
                    continue;
                case NodeState.Success:
                    // 하나라도 성공하면 즉시 성공 반환
                    return NodeState.Success;
                case NodeState.Running:
                    // 하나라도 실행 중이면 전체가 실행 중
                    return NodeState.Running;
            }
        }
        // 모든 자식이 실패했을 때만 실패 반환
        return NodeState.Failure;
    }
    
    
    public class CheckPlayerInSightNode : Node
    {
        private readonly CharacterController self;
        private readonly CharacterController player;
        private readonly float sightRange;

        public CheckPlayerInSightNode(CharacterController self, CharacterController player, float sightRange)
        {
            this.self = self;
            this.player = player;
            this.sightRange = sightRange;
        }

        public override NodeState Evaluate()
        {
            if (Vector2Int.Distance(self.Astar.CurrentNode.Position, player.Astar.CurrentNode.Position) <= sightRange && player.gameObject.layer == 8)
            {
                Debug.Log("플레이어 발견!"); // 여기서 반복됨.
                return NodeState.Success;
            }
            return NodeState.Failure;
        }
    }

    public class ChaseNode : Node
    {
        private readonly ChaserController self;
        private readonly CharacterController player;
        private readonly float sightRange;
        private Action chaseAction;
        bool isChasing = false; // 추적 상태를 나타내는 변수

        public ChaseNode(ChaserController self, CharacterController player, float sightRange, Action chaseAction)
        {
            this.self = self;
            this.player = player;
            this.sightRange = sightRange;
            this.chaseAction = chaseAction;
        }
        public override NodeState Evaluate()
        {

            // 플레이어가 시야에 있고, 레이어가 8일 때
            if (Vector2Int.Distance(self.Astar.CurrentNode.Position, player.Astar.CurrentNode.Position) <= sightRange && player.gameObject.layer == 8)
            {
                // 만약 추적 상태가 아니었다면, 상태를 변경하고 추적 시작
                if (!isChasing)
                {
                    self.SetStatus(); // CharacterStatus를 Moving으로 설정
                    isChasing = true; // 추적 상태로 변경
                }
                chaseAction(); // Chase() 메서드 호출 (경로 계산 및 이동)

                // 만약 Chase() 결과 경로를 못찾았다면 추적 실패
                if (self.Astar.Path.Count == 0)
                {
                    isChasing = false; // 추적 상태 해제
                    return NodeState.Failure; // 실패 반환하여 다른 행동(순찰)을 하도록 유도
                }
                return NodeState.Running; // 추적이 계속 진행 중
            }

            // 플레이어가 시야에 없으면 추적 종료
            if (isChasing)
            {
                isChasing = false; // 추적 상태 해제
            }
            return NodeState.Success; // Success를 반환하여 다음 프레임에 PatrolNode가 자연스럽게 실행되도록 함
        }
    }
    
    public class RunAwayNode : Node
    {
        

        private readonly RunnerController self;
        private readonly ChaserController player;
        private Action runAction;
        private readonly int runDistance;
        public RunAwayNode(RunnerController self, ChaserController player, int runDistance, Action action)
        {
            this.self = self;
            this.player = player;
            this.runDistance = runDistance;
            runAction = action;
        }
        public override NodeState Evaluate()
        {
            

            long distance = self.Astar.Path.Count;

            if(distance < runDistance)
            {
                runAction(); // 도망치는 행동 실행
                return NodeState.Running;
            }

            if(distance >= runDistance)
            {
                self.ResetRun();
                return NodeState.Success; // 충분히 도망쳤다면 성공
            }
            return NodeState.Failure; // 도망칠 수 없음
        }
    }
    
    public class PatrolNode : Node
    {
        private CharacterController self;
        private Action move;
        private Action setDestination;
        public PatrolNode(CharacterController _self, Action _move, Action _setDestination) {
            self = _self;
            move = _move;
            setDestination = _setDestination;
        }
    
        public override NodeState Evaluate()
        {
            if(self.Astar.Path.Count <= 0)
            {
                self.Astar.StartPos = self.Astar.CurrentNode.Position; // 현재 위치를 시작 위치로 설정
                setDestination(); // 새로운 목적지 설정
                return NodeState.Success;
            }
            move(); // 현재 목적지로 이동
            return NodeState.Running; 
        }
    }

}

