using UnityEngine;
using System.Collections.Generic;

public class RailSegment : MonoBehaviour
{
    [Header("轨道端口 (两个球形触发器)")]
    public SphereCollider portStart; 
    public SphereCollider portEnd;

    // 获取距离给定点最近的端口
    public Collider GetClosestPort(Vector3 pos)
    {
        float dStart = Vector3.Distance(portStart.transform.position, pos);
        float dEnd = Vector3.Distance(portEnd.transform.position, pos);
        return dStart < dEnd ? portStart : portEnd;
    }
}