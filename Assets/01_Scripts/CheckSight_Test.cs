using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class CheckSight_Test : MonoBehaviour
{
    [SerializeField]
    private int sightRadius; //시야 범위 지정
    [SerializeField]
    private Vector2 targetrPos;//타겟 위치
    private bool targetFound; //타겟 발견 여부








    public void FindTarget(Vector2 _pos)
    {
        targetFound = true;
        targetrPos = _pos;
    }
}
