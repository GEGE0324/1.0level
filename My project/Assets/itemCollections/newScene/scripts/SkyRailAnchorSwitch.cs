using System.Collections;
using UnityEngine;
using Opsive.Shared.Events;

public class SkyRailAnchorSwitch : MonoBehaviour
{
    [Header("子物体")]
    public GameObject onChild;
    public GameObject offChild;
    public float onRotateSpeed = 360f;

    [Header("场景中要旋转的物体")]
    public Transform targetObject;
    public float rotateDuration = 0.5f;

    private bool onActive = false;
    private Coroutine targetRotateCoroutine;
    private Coroutine childRotateCoroutine;

    private void Awake()
    {
        // 只监听伤害事件，参数必须匹配这 5 个：伤害量, 位置, 力量, 攻击者, 碰撞体
        EventHandler.RegisterEvent<float, Vector3, Vector3, GameObject, Collider>(gameObject, "OnHealthDamage", OnDamage);
    }

    // 无论远程近战，只要造成了伤害就会进这里
    private void OnDamage(float amount, Vector3 position, Vector3 force, GameObject attacker, Collider hitCollider)
    {
        HandleHit();
    }

    void HandleHit()
    {
        // 状态切换逻辑：第一次被砍时激活显示
        if (!onActive)
        {
            onChild?.SetActive(true);
            offChild?.SetActive(false);
            onActive = true;
        }

        // --- 核心改动：支持多次触发 ---

        // 1. targetObject 旋转逻辑
        // 不再判断 isRotating，只要被砍就重新计算并启动旋转
        if (targetObject != null)
        {
            if (targetRotateCoroutine != null) StopCoroutine(targetRotateCoroutine);
            targetRotateCoroutine = StartCoroutine(RotateTargetY(90f));
        }

        // 2. onChild 旋转逻辑
        if (onChild != null)
        {
            if (childRotateCoroutine != null) StopCoroutine(childRotateCoroutine);
            childRotateCoroutine = StartCoroutine(RotateChildY(180f));
        }
    }

    IEnumerator RotateTargetY(float angle)
    {
        float rotated = 0f;
        float speed = angle / rotateDuration;
        while (rotated < angle)
        {
            float step = speed * Time.deltaTime;
            if (rotated + step > angle) step = angle - rotated;
            targetObject.Rotate(Vector3.up * step);
            rotated += step;
            yield return null;
        }
        Vector3 euler = targetObject.eulerAngles;
        euler.y = Mathf.Round(euler.y / 90f) * 90f;
        targetObject.eulerAngles = euler;
    }

    IEnumerator RotateChildY(float angle)
    {
        float rotated = 0f;
        while (rotated < angle)
        {
            float step = onRotateSpeed * Time.deltaTime;
            if (rotated + step > angle) step = angle - rotated;
            onChild.transform.Rotate(Vector3.up * step);
            rotated += step;
            yield return null;
        }
    }

    private void OnDestroy()
    {
        EventHandler.UnregisterEvent<float, Vector3, Vector3, GameObject, Collider>(gameObject, "OnHealthDamage", OnDamage);
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }
}