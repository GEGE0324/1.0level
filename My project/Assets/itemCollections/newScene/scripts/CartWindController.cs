using UnityEngine;
using UnityEngine.Splines;

public class CartWindController : MonoBehaviour
{
    [Header("轨道配置")]
    public SplineContainer railSpline;
    public float detectionRadius = 2.0f;
    public float switchCooldown = 0.5f;

    [Header("动力参数")]
    public float currentSpeed = 0f;
    public float friction = 0.5f;
    public float maxSpeed = 15f;

    private float distanceTraveled = 0f;
    private float cooldownTimer = 0f;
    private SplineContainer lastSpline;
    private int moveDirection = 1;

    private SplineContainer initialSpline;

    void Start()
    {
        initialSpline = railSpline;
    }

    void Update()
    {
        if (railSpline == null) return;
        if (cooldownTimer > 0) cooldownTimer -= Time.deltaTime;

        float splineLength = railSpline.CalculateLength();

        currentSpeed = Mathf.Lerp(currentSpeed, 0, friction * Time.deltaTime);

        // 1. 位移计算
        if (Mathf.Abs(currentSpeed) > 0.01f)
        {
            distanceTraveled += (currentSpeed * moveDirection) * Time.deltaTime;
        }

        // --- 彻底重置逻辑：防止回弹 ---
        if (railSpline == initialSpline && distanceTraveled <= 0.3f)
        {
            // 当小车从回程（-1）状态回到起点，且距离非常近时
            if (moveDirection == -1)
            {
                moveDirection = 1;     // 恢复正向
                distanceTraveled = 0;  // 坐标归位
                currentSpeed = 0;      // 强制刹车，抹除回程惯性
                lastSpline = null;
                Debug.Log("<color=orange>【系统】回到起点：速度已清零，逻辑已重置</color>");
            }
        }

        // 2. 边界换轨判定
        if (distanceTraveled >= splineLength || distanceTraveled <= 0)
        {
            if (cooldownTimer <= 0)
            {
                if (!TrySwitchToNextSpline())
                {
                    distanceTraveled = Mathf.Clamp(distanceTraveled, 0, splineLength);
                    currentSpeed = 0;
                }
            }
            else
            {
                distanceTraveled = Mathf.Clamp(distanceTraveled, 0, splineLength);
            }
        }

        UpdateCartPosition(splineLength);
    }

    private void UpdateCartPosition(float splineLength)
    {
        float normalizedPos = Mathf.Clamp01(distanceTraveled / splineLength);
        transform.position = railSpline.EvaluatePosition(normalizedPos);
    }

    private bool TrySwitchToNextSpline()
    {
        Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, detectionRadius);
        foreach (var col in nearbyColliders)
        {
            SplineContainer nextSpline = col.GetComponentInParent<SplineContainer>();
            if (nextSpline != null && nextSpline != railSpline)
            {
                if (cooldownTimer > 0 && nextSpline == lastSpline) continue;

                float nextLength = nextSpline.CalculateLength();
                Vector3 startPos = nextSpline.EvaluatePosition(0);
                Vector3 endPos = nextSpline.EvaluatePosition(1);

                if (Vector3.Distance(transform.position, startPos) < detectionRadius)
                {
                    PerformSwitch(nextSpline, 0f, 1);
                    return true;
                }
                else if (Vector3.Distance(transform.position, endPos) < detectionRadius)
                {
                    PerformSwitch(nextSpline, nextLength, -1);
                    return true;
                }
            }
        }
        return false;
    }

    private void PerformSwitch(SplineContainer next, float newDist, int newDir)
    {
        lastSpline = railSpline;
        railSpline = next;
        distanceTraveled = newDist;
        moveDirection = newDir;
        cooldownTimer = switchCooldown;

        // 换轨瞬间推力
        currentSpeed = 10f;
        Debug.Log($"<color=lime>换轨成功: {next.name} | 模式: {newDir}</color>");
    }

    public void AddWindForce(float force)
    {
        currentSpeed += force;
        currentSpeed = Mathf.Clamp(currentSpeed, -maxSpeed, maxSpeed);
    }
}