using NUnit.Framework;
using System;
using UnityEngine;
using System.Collections.Generic;

using Random = UnityEngine.Random;
using System.Linq;
using Unity.IO.LowLevel.Unsafe;
using Unity.VisualScripting;

public class CharacterMove : MonoBehaviour
{
    private RaycastHit hit;
    private float speed = 5f; //캐릭터 속도
    public Transform CurrentTransform; //현재 위치
    [SerializeField]
    private Vector2Int destination; //목표 위치
    private Transform start; //시작 위치
    public Transform[,] Tiles; //타일 배열
    public bool Start;
    public Vector2Int LocalPos = Vector2Int.zero; //캐릭터의 로컬 좌표
    [SerializeField]
    private List<Vector2Int> pathVec = new List<Vector2Int>(); //경로 리스트
    [SerializeField]    
    private List<Transform> path = new List<Transform>(); //경로 리스트
    public List<Vector2Int> DismovableTiles = new List<Vector2Int>();
    public Vector2 GroundScale = new Vector2(0.2f, 0.2f); //바닥 크기


    [SerializeField]
    protected List<Vector2Int> emptyNodes = new List<Vector2Int>();
    [SerializeField]
    protected List<Vector2Int> blockNodes = new List<Vector2Int>();
    [SerializeField]
    protected List<Vector2Int> noWay = new List<Vector2Int>();
    [SerializeField]
    protected int movementCount = 0; //이동 카운트
    [SerializeField]
    protected int currentMovementCount = 0; //현재 이동 카운트

    private float moveTime;


    protected virtual void Update()
    {
        if (!Start)
        {
            return;
        }
        CheckNearNodes();

    }




    public virtual void SetDestination()
    {
        pathVec.Clear();
        path.Clear();
        currentMovementCount = 0;
        movementCount = 0;
        Random.InitState((int)DateTime.Now.Ticks); //랜덤 시드 초기화
        destination = new Vector2Int(Random.Range(0, Tiling.Instance.Tiles.GetLength(0) - 1), Random.Range(0, Tiling.Instance.Tiles.GetLength(1) - 1));
        while (DismovableTiles.Contains(destination))
        {
            destination = new Vector2Int(Random.Range(0, Tiling.Instance.Tiles.GetLength(0) - 1), Random.Range(0, Tiling.Instance.Tiles.GetLength(1) - 1));
        }
        Start = true;
    }

    protected virtual void CheckNearNodes()
    {
        if (pathVec.Count <= 0)
        {
            pathVec.Add(LocalPos);
            path.Add(Tiles[LocalPos.x, LocalPos.y]);
        }

        if (pathVec[movementCount] == destination)
        {
            Move();
            return;
        }

        if (pathVec[movementCount].x >= 1)
        {
            if (DismovableTiles.Contains(pathVec[movementCount] - new Vector2Int(1, 0)))
            {
                blockNodes.Add(pathVec[movementCount] - new Vector2Int(1, 0));
            }
            else
            {
                emptyNodes.Add(pathVec[movementCount] - new Vector2Int(1, 0));
            }
        }
        if (pathVec[movementCount].x < Tiles.GetLength(0) - 1)
        {
            if (DismovableTiles.Contains(pathVec[movementCount] + new Vector2Int(1, 0)))
            {
                blockNodes.Add(pathVec[movementCount] + new Vector2Int(1, 0));
            }
            else
            {
                emptyNodes.Add(pathVec[movementCount] + new Vector2Int(1, 0));
            }
        }
        if (pathVec[movementCount].y >= 1)
        {
            if (DismovableTiles.Contains(pathVec[movementCount] - new Vector2Int(0, 1)))
            {
                blockNodes.Add(pathVec[movementCount] - new Vector2Int(0, 1));
            }
            else
            {
                emptyNodes.Add(pathVec[movementCount] - new Vector2Int(0, 1));
            }
        }
        if (pathVec[movementCount].y < Tiles.GetLength(1) - 1)
        {
            if (DismovableTiles.Contains(pathVec[movementCount] + new Vector2Int(0, 1)))
            {
                blockNodes.Add(pathVec[movementCount] + new Vector2Int(0, 1));
            }
            else
            {
                emptyNodes.Add(pathVec[movementCount] + new Vector2Int(0, 1));
            }
        }
        AStarAlgorithm();
    }

    protected virtual void AStarAlgorithm()
    {
        float length = int.MaxValue;
        Vector2Int nextNode = Vector2Int.zero;
        for (int i = 0; i < emptyNodes.Count; i++)
        {
            if (noWay.Contains(emptyNodes[i]))
            {
                continue;
            }
            if (pathVec.Contains(emptyNodes[i]))
            {
                continue;
            }
            if (length > Vector2.Distance(emptyNodes[i], destination))
            {
                length = Vector2.Distance(emptyNodes[i], destination);
                nextNode = emptyNodes[i];
            }

        }
        if (length >= int.MaxValue)
        {
            movementCount--;
            noWay.Add(pathVec[movementCount + 1]);
            pathVec.Remove(pathVec[movementCount + 1]);
            path.Remove(path[movementCount + 1]);
        }
        else
        {
            pathVec.Add(nextNode);
            path.Add(Tiles[nextNode.x, nextNode.y]);
            movementCount++;
        }
        emptyNodes.Clear();
        blockNodes.Clear();
    }

    protected virtual void Move()
    {
        if (pathVec.Count <= 0)
        {
            return;
        }
        transform.eulerAngles = new Vector3(pathVec[currentMovementCount].x - LocalPos.x, 0, pathVec[currentMovementCount].y - LocalPos.y);
        transform.Translate((Tiles[pathVec[currentMovementCount].x, pathVec[currentMovementCount].y].position - Tiles[LocalPos.x, LocalPos.y].position) * 0.2f);
        
        Physics.Raycast(transform.position, Vector3.down, out hit, 100f, 1 << 7);

        //for (int i = 0; i < Tiles.GetLength(0); i++)
        //{
        //    for(int j = 0; j < Tiles.GetLength(1); j++)
        //    {
                
        //    }
        //}
        Debug.Log(hit.transform.name);
        if (path[currentMovementCount].name == hit.transform.name)
        {
            transform.position = (Tiles[pathVec[currentMovementCount].x, pathVec[currentMovementCount].y].position + new Vector3(0, 0.5f, 0));
            LocalPos = pathVec[currentMovementCount];
            currentMovementCount++;
        }
        if(currentMovementCount > movementCount)
        {
            currentMovementCount = movementCount;
            SetDestination();
        }
    }
}
