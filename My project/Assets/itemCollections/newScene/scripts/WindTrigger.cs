using System.Collections;
using UnityEngine;
using Opsive.Shared.Events;

public class WindTrigger : MonoBehaviour
{
    [Header("关联设置")]
    public CartWindController cart; // 关联主体小车

    [Header("速度设置")]
    public float targetSpeed = 10f;    // 最终达到的稳定速度
    public float accelerationTime = 2f; // 从当前速度增加到目标速度所需的时间

    private Coroutine speedCoroutine;

    private void Awake()
    {
        // 注册 UCC 伤害事件
        EventHandler.RegisterEvent<float, Vector3, Vector3, GameObject, Collider>(gameObject, "OnHealthDamage", OnDamage);
    }

    private void OnDamage(float amount, Vector3 position, Vector3 force, GameObject attacker, Collider hitCollider)
    {
        OnBeaten();
    }

    public void OnBeaten()
    {
        if (cart == null) return;

        // 如果已经在加速中，停止旧的，开始新的（或者继续加速）
        if (speedCoroutine != null) StopCoroutine(speedCoroutine);
        speedCoroutine = StartCoroutine(AccelerateCart());

        Debug.Log("击中风扇：小车开始平滑加速至 " + targetSpeed);
    }

    IEnumerator AccelerateCart()
    {
        // 假设 CartWindController 有一个控制当前速度的变量，比如叫 currentSpeed
        // 注意：这里需要确保你的 CartWindController 里有对应的设置速度的方法或变量

        float initialSpeed = cart.currentSpeed; // 获取小车当前速度
        float elapsed = 0f;

        while (elapsed < accelerationTime)
        {
            elapsed += Time.deltaTime;
            float ratio = elapsed / accelerationTime;

            // 使用 Lerp 实现从慢到快的平滑速度过渡
            float newSpeed = Mathf.Lerp(initialSpeed, targetSpeed, ratio);

            // 这里调用小车的方法来设置速度
            // 如果小车的变量名不同，请修改这里
            cart.currentSpeed = newSpeed;

            yield return null;
        }

        // 确保最终达到稳定速度值
        cart.currentSpeed = targetSpeed;
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