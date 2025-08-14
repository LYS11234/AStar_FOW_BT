using UnityEngine;

public struct ConstDataType
{
    public const byte hasLineOfSight = 0; // 시야 확인 변수
    public const byte action = 1; // 행동 변수
    public const byte fow = 2; // 시야 변수
    public const byte fowSight = 3; // 시야 변수
}

public struct ConstTypes
{
    public const byte FOW = 0; // FOW 타입
    public const byte Tiling = 1; // Tiling 타입
}
public struct ConstVariables
{
    public const int Resolution = 1024; // 해상도 변수
}


public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    [SerializeField]
    public FOW fow;
    [SerializeField]
    public Tiling tiling;


    public void RegisterObserver(ISubject subject)
    {
        subject.RegisterObserver(fow);
    }
}
