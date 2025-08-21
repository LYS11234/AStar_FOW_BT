using System;
using UnityEngine;
using System.Collections.Generic;

using Random = UnityEngine.Random;
using System.Linq;


public class TileData : IComparable<TileData>
{
    public Vector2Int Position; //타일 위치
    public int FValue;
    public int GValue;
    public int HValue;

    public bool IsBlock; //열린 타일 여부

    public TileData Parent; //부모 타일
    public string Name; //타일 이름

    public TileData()
    {
        Position = new Vector2Int(0, 0);
        FValue = 0;
        GValue = 0;
        HValue = 0;
        IsBlock = false;
        Parent = null;
        Name = string.Empty;
    }
    public int CompareTo(TileData other)
    {
        if (other == null) return 1; // null인 경우 현재 객체가 더 큼
        if (FValue == other.FValue) return 0; // FValue가 같으면 0 반환
        return FValue.CompareTo(other.FValue); // FValue를 기준으로 비교
    }
}


public class Tiling : MonoBehaviour
{
    [SerializeField]
    private FOW fogOfWar;

    private GameManager gameManager; //게임 매니저
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
    public List<Vector2Int> DismovableTiles = new List<Vector2Int>(); //이동 불가능한 타일 리스트


    void Start()
    {
        gameManager = GameManager.Instance; //게임 매니저 인스턴스 가져오기
        width = transform.localScale.x;//오브젝트의 너비
        height = transform.localScale.z; //오브젝트의 높이
        tileWidth = 0.2f; //타일의 너비
        tileHeight = 0.2f; //타일의 높이

        int xCount = Mathf.CeilToInt(width / tileWidth); //x축 타일 개수
        int zCount = Mathf.CeilToInt(height / tileHeight); //z축 타일 개수
        Tiles = new Transform[xCount, zCount]; //타일 배열 초기화
        TileDataArray = new TileData[xCount, zCount]; //타일 데이터 배열 초기화
        GenerateTile(xCount, zCount); //타일 생성
        fogOfWar.Init(xCount, zCount);
    }


    private void GenerateTile(int _xCount, int _yCount)
    {
        var xRange = Enumerable.Range(0, _xCount);
        var yRange = Enumerable.Range(0, _yCount);
        var _tiles = xRange.SelectMany(x => yRange.Select(y => new Vector2Int(x, y))).ToArray(); // 타겟 타일 리스트 생성

        foreach(var tile in _tiles)
        {
            Tiles[tile.x, tile.y] = Instantiate(Resources.Load<GameObject>("Tile")).transform;
            Tiles[tile.x, tile.y].name = $"Tile{tile.x}_{tile.y}"; //타일 이름 설정
            TileDataArray[tile.x, tile.y] = new TileData(); //타일 데이터 초기화
            TileDataArray[tile.x, tile.y].Position = new Vector2Int(tile.x, tile.y); //타일 위치 설정
            TileDataArray[tile.x, tile.y].Name = Tiles[tile.x, tile.y].name; //타일 이름 설정
            Tiles[tile.x, tile.y].position = new Vector3(tile.x * 2 + 1, 0, tile.y * 2 + 1); //타일 위치 설정
            Tiles[tile.x, tile.y].localScale = new Vector3(tileWidth, 1, tileHeight); //타일 크기 설정
            Tiles[tile.x, tile.y].parent = transform; //타일 부모 설정
            Tiles[tile.x, tile.y].gameObject.layer = 7; //타일 레이어 설정
            //레이캐스트로 장애물 찾기
            if (!Physics.Raycast(Tiles[tile.x, tile.y].position - new Vector3(0, 10, 0), Vector3.up, out RaycastHit hit, 100f, 1 << 6))
            {
                continue; //장애물이 없으면 다음 타일로
            }
            TileDataArray[tile.x, tile.y].IsBlock = true; //장애물 타일 설정
        }
        GenerateCharacters();
    }

    private void GenerateCharacters()
    {
        Random.InitState((int)DateTime.Now.Ticks); //랜덤 시드 초기화
        GameObject _chaser = Instantiate(Resources.Load<GameObject>("Chaser"));
        
        _chaser.GetComponent<ChaserController>().Tiles = Tiles; //타일 배열 설정
        _chaser.GetComponent<ChaserController>().Astar.TileDataList = TileDataArray; //타일 데이터 배열 설정
        _chaser.GetComponent<ChaserController>().Astar.StartPos = new Vector2Int(chaserStartPoint[Random.Range(0, chaserStartPoint.Length)].x, chaserStartPoint[Random.Range(0, chaserStartPoint.Length)].y); //추적자 시작 위치 설정
        _chaser.transform.position = Tiles[_chaser.GetComponent<ChaserController>().Astar.StartPos.x, _chaser.GetComponent<CharacterController>().Astar.StartPos.y].position + new Vector3(0, 0.5f, 0); //추적자 위치 설정
        


        GameObject _runner = Instantiate(Resources.Load<GameObject>("Runner"));
        _runner.GetComponent<RunnerController>().Tiles = Tiles; //타일 배열 설정
        _runner.GetComponent<RunnerController>().Astar.TileDataList = TileDataArray; //타일 데이터 배열 설정
        _runner.GetComponent<RunnerController>().Astar.StartPos = new Vector2Int(runnerStartPoint[Random.Range(0, runnerStartPoint.Length)].x, runnerStartPoint[Random.Range(0, runnerStartPoint.Length)].y); //도망자 시작 위치 설정
        _runner.transform.position = Tiles[_runner.GetComponent<RunnerController>().Astar.StartPos.x, _runner.GetComponent<CharacterController>().Astar.StartPos.y].position + new Vector3(0, 0.5f, 0); //도망자 위치 설정
        gameManager.fow.Characters[0] = _chaser.GetComponent<CharacterController>();
        gameManager.fow.Characters[1] = _runner.GetComponent<CharacterController>();
        gameManager.RegisterObserver(_chaser.GetComponent<ChaserController>()); //추적자에 옵저버 등록
        gameManager.RegisterObserver(_runner.GetComponent<RunnerController>()); //도망자에 옵저버 등록
        _runner.GetComponent<RunnerController>().Init(_chaser.GetComponent<ChaserController>(), gameManager.fow.viewAngle); //목표 위치 설정

        _chaser.GetComponent<ChaserController>().Init(_runner.GetComponent<RunnerController>()); //목표 위치 설정
        
        
        
        
    }
}
