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

    // ===== 关键新增 =====
    private int rotateIndex = 0;   // 当前档位（0~3）
    private float baseY;           // 初始Y角度（可不是0）

    private void Awake()
    {
        if (targetObject != null)
            baseY = targetObject.eulerAngles.y;

        EventHandler.RegisterEvent<float, Vector3, Vector3, GameObject, Collider>(
            gameObject, "OnHealthDamage", OnDamage);
    }

    // 无论远程近战，只要造成了伤害就会进这里
    private void OnDamage(float amount, Vector3 position, Vector3 force, GameObject attacker, Collider hitCollider)
    {
        HandleHit();
    }

    void HandleHit()
    {
        // 第一次激活显示
        if (!onActive)
        {
            onChild?.SetActive(true);
            offChild?.SetActive(false);
            onActive = true;
        }

        // ===== 目标物体：90°档位旋转 =====
        if (targetObject != null)
        {
            rotateIndex = (rotateIndex + 1) % 4;
            float targetY = baseY + rotateIndex * 90f;

            if (targetRotateCoroutine != null)
                StopCoroutine(targetRotateCoroutine);

            targetRotateCoroutine = StartCoroutine(RotateTargetToY(targetY));
        }

        // ===== 子物体：持续旋转 =====
        if (onChild != null)
        {
            if (childRotateCoroutine != null)
                StopCoroutine(childRotateCoroutine);

            childRotateCoroutine = StartCoroutine(RotateChildY(180f));
        }
    }

    // ================= 目标物体旋转（稳定） =================
    IEnumerator RotateTargetToY(float targetY)
    {
        float startY = targetObject.eulerAngles.y;
        float elapsed = 0f;

        while (elapsed < rotateDuration)
        {
            float t = elapsed / rotateDuration;
            float y = Mathf.LerpAngle(startY, targetY, t);

            Vector3 euler = targetObject.eulerAngles;
            euler.y = y;
            targetObject.eulerAngles = euler;

            elapsed += Time.deltaTime;
            yield return null;
        }

        Vector3 finalEuler = targetObject.eulerAngles;
        finalEuler.y = targetY;
        targetObject.eulerAngles = finalEuler;
    }

    // ================= 子物体旋转 =================
    IEnumerator RotateChildY(float angle)
    {
        float rotated = 0f;

        while (rotated < angle)
        {
            float step = onRotateSpeed * Time.deltaTime;
            if (rotated + step > angle)
                step = angle - rotated;

            onChild.transform.Rotate(Vector3.up * step);
            rotated += step;
            yield return null;
        }
    }

    private void OnDestroy()
    {
        EventHandler.UnregisterEvent<float, Vector3, Vector3, GameObject, Collider>(
            gameObject, "OnHealthDamage", OnDamage);
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }
}
