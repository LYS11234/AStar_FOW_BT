using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class CheckSight_Test : MonoBehaviour
{
    [SerializeField]
    private int sightRadius; //�þ� ���� ����
    [SerializeField]
    private Vector2 targetrPos;//Ÿ�� ��ġ
    private bool targetFound; //Ÿ�� �߰� ����








    public void FindTarget(Vector2 _pos)
    {
        targetFound = true;
        targetrPos = _pos;
    }
}
