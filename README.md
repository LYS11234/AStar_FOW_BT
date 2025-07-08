# AStar_FOW_BT

Fog OF War + AStar + Behavior Tree



Fog Of War: 텍스쳐를 타일단위로 쪼개서 연산. 유닛의 현재 타일을 시작으로 원형의 범위를 계산, 내적을 이용해 시야 범위를 연산한 뒤, LineCast로 벽과의 충돌을 감지, 텍스쳐의 A값을 변경함.



A\*: G + H = F 를 이용해 타일데이터 배열에서 타일데이터를 빠르게 읽어와 연산, 목적지까지의 최단경로를 계산, 경로 List에 저장 후 Reverse로 경로를 확정, 이동함



Behavior Tree: 클래스로 나누어 실패 반환 시 처음부터 실행되도록 만듦. CheckPlayerInSightNode에서 타겟 유닛 탐지 -> 탐지시 Chase나 RunAway로, 아니면 Patrol로 동작하도록

