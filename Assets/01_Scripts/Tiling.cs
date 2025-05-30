using NUnit.Framework;
using System;
using Unity.Mathematics;
using UnityEngine;
using System.Collections.Generic;

using Random = UnityEngine.Random;

[System.Serializable]
public class TileData
{
    public Vector2Int Position; //타일 위치
    public int FValue;
    public int GValue;
    public int HValue;

    public bool IsBlock; //열린 타일 여부

    public TileData Parent; //부모 타일
    public string Name; //타일 이름
}


public class Tiling : MonoBehaviour
{
    public static Tiling Instance; //싱글톤 인스턴스
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this; //싱글톤 인스턴스 설정
        }
        else
        {
            Destroy(gameObject); //중복 인스턴스 삭제
        }
    }


    private float width; //오브젝트의 너비
    private float height; //오브젝트의 높이

    private float tileWidth; //타일의 너비
    private float tileHeight; //타일의 높이

    [SerializeField]
    private Vector2Int[] chaserStartPoint;
    [SerializeField]
    private Vector2Int[] runnerStartPoint;

    public TileData[,] TileDataArray; //타일 데이터 배열

    public Transform[ , ] Tiles; //타일 배열 x,z
    [SerializeField]
    public List<Vector2Int> DismovableTiles = new List<Vector2Int>(); //이동 불가능한 타일 리스트


    void Start()
    {
        width = transform.localScale.x;//오브젝트의 너비
        height = transform.localScale.z; //오브젝트의 높이
        tileWidth = 0.2f; //타일의 너비
        tileHeight = 0.2f; //타일의 높이

        int xCount = Mathf.CeilToInt(width / tileWidth); //x축 타일 개수
        int zCount = Mathf.CeilToInt(height / tileHeight); //z축 타일 개수
        Tiles = new Transform[xCount, zCount]; //타일 배열 초기화
        TileDataArray = new TileData[xCount, zCount]; //타일 데이터 배열 초기화
        GenerateTile(xCount, zCount); //타일 생성
    }


    private void GenerateTile(int _xCount, int _yCount)
    {
        for(int i = 0; i < _xCount; i++)
        {
            for (int j = 0; j < _yCount; j++)
            {
                Tiles[i, j] = Instantiate<GameObject>(Resources.Load<GameObject>("Tile")).transform;
                Tiles[i, j].name = $"Tile{i}_{j}"; //타일 이름 설정
                TileDataArray[i, j] = new TileData(); //타일 데이터 초기화
                TileDataArray[i, j].Position = new Vector2Int(i, j); //타일 위치 설정
                TileDataArray[i, j].Name = Tiles[i, j].name; //타일 이름 설정
                TileDataArray[i, j].IsBlock = false; //타일 장애물 여부 초기화
                Tiles[i, j].position = new Vector3(i * 2 + 1, 0, j * 2 + 1); //타일 위치 설정
                Tiles[i, j].localScale = new Vector3(tileWidth, 1, tileHeight); //타일 크기 설정
                Tiles[i, j].parent = transform; //타일 부모 설정
                Tiles[i, j].gameObject.layer = 7;
                                                                                        //레이캐스트로 장애물 찾기

                if (Physics.Raycast(Tiles[i, j].position - new Vector3(0,10,0) , Vector3.up, out RaycastHit hit, 100f,1 << 6))
                {
                    TileDataArray[i, j].IsBlock = true; //장애물 타일 설정

                }
                 //이동 불가능한 타일 리스트에 추가
            }
        }
        GenerateCharacters();
    }

    private void GenerateCharacters()
    {
        Random.InitState((int)DateTime.Now.Ticks); //랜덤 시드 초기화
        GameObject _chaser = Instantiate<GameObject>(Resources.Load<GameObject>("Chaser"));
        
        _chaser.GetComponent<CharacterController>().Tiles = Tiles; //타일 배열 설정
        _chaser.GetComponent<CharacterController>().TileDatas = TileDataArray; //타일 데이터 배열 설정
        _chaser.GetComponent<CharacterController>().StartPos = new Vector2Int(chaserStartPoint[Random.Range(0, chaserStartPoint.Length)].x, chaserStartPoint[Random.Range(0, chaserStartPoint.Length)].y); //추적자 시작 위치 설정
        _chaser.transform.position = Tiles[_chaser.GetComponent<CharacterController>().StartPos.x, _chaser.GetComponent<CharacterController>().StartPos.y].position + new Vector3(0, 0.5f, 0); //추적자 위치 설정
        _chaser.GetComponent<CharacterController>().SetDestination(); //목표 위치 설정


        GameObject _runner = Instantiate<GameObject>(Resources.Load<GameObject>("Runner"));
        _runner.GetComponent<CharacterController>().Tiles = Tiles; //타일 배열 설정
        _runner.GetComponent<CharacterController>().TileDatas = TileDataArray; //타일 데이터 배열 설정
        _runner.GetComponent<CharacterController>().StartPos = new Vector2Int(runnerStartPoint[Random.Range(0, runnerStartPoint.Length)].x, runnerStartPoint[Random.Range(0, runnerStartPoint.Length)].y); //도망자 시작 위치 설정
        _runner.transform.position = Tiles[_runner.GetComponent<CharacterController>().StartPos.x, _runner.GetComponent<CharacterController>().StartPos.y].position + new Vector3(0, 0.5f, 0); //도망자 위치 설정
        _runner.GetComponent<CharacterController>().SetDestination(); //목표 위치 설정

    }
}
