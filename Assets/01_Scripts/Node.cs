using NUnit.Framework.Constraints;
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
            self.HasLineOfSight(); // 플레이어가 시야에 있는지 확인하는 메서드 호출
            if (!self.GetInSight())
            {
                return NodeState.Failure; // 시야 내에 플레이어가 없으면 실패 반환
            }// 시야 내의 타일을 업데이트
            if (Vector2Int.Distance(self.Astar.CurrentNode.Position, player.Astar.CurrentNode.Position) <= sightRange)
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
            if (!self.GetState()) 
            {
                self.SetStatus(); // CharacterStatus를 Moving으로 설정
            }
            if (self.GetState() && self.Astar.Path[self.Astar.Path.Count - 1] == self.Astar.CurrentNode) // 현재 노드가 목적지와 같으면
            {
                self.EndChase(); // EndChase() 메서드 호출 (목적지 설정)
                isChasing = false; // 추적 상태 해제
            }
            chaseAction(); // Chase() 메서드 호출 (경로 계산 및 이동)

            // 만약 Chase() 결과 경로를 못찾았다면 추적 실패
            if (self.Astar.Path.Count == 0 )
            {
                if (self.GetState())
                {
                    self.EndChase(); // EndChase() 메서드 호출 (목적지 설정)
                    isChasing = false; // 추적 상태 해제
                }
                return NodeState.Failure; // 실패 반환하여 다른 행동(순찰)을 하도록 유도
            }
            return NodeState.Running; // 추적이 계속 진행 중

            // 플레이어가 시야에 없으면 추적 종료

            return NodeState.Success; // Success를 반환하여 다음 프레임에 PatrolNode가 자연스럽게 실행되도록 함

        }
    }
    public class RunAwayNode : Node
    {
        

        private readonly RunnerController self;
        private readonly ChaserController player;
        private Action runAction;

        public RunAwayNode(RunnerController self, ChaserController player, Action action)
        {
            this.self = self;
            this.player = player;
            runAction = action;
        }
        public override NodeState Evaluate()
        {
            // 'isRunning' 플래그가 켜져 있을 때만 이 노드가 작동하도록 변경
            if (self.IsRunning())
            {
                if (self.GetCurrentRunTime() > 0f)
                {
                    runAction(); // 도망치는 행동 실행
                    return NodeState.Running;
                }
                else // 도망 시간이 끝나면
                {
                    self.ResetRun();
                    Debug.Log("도주 종료");
                    return NodeState.Success;
                }
            }

            // 도망치는 상태가 아니면 이 노드는 관여하지 않음
            return NodeState.Failure;
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
                if(self.TryGetComponent<ChaserController>(out ChaserController chaser))
                {
                    Debug.Log("추적자 순찰 중");
                }
                return NodeState.Success;
            }
            move(); // 현재 목적지로 이동
            return NodeState.Running; 
        }
    }

    public class ActionNode : Node
    {
        private readonly Action action;

        public ActionNode(Action action)
        {
            this.action = action;
        }

        public override NodeState Evaluate()
        {
            action();
            return NodeState.Success;
        }
    }

}

