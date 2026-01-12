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

    [Header("转向设置")]
    public float rotationSmoothTime = 10f;

    private float distanceTraveled = 0f;
    private float cooldownTimer = 0f;
    private SplineContainer lastSpline;
    private int moveDirection = 1;

    private SplineContainer initialSpline;
    private Vector3 lastTangent; // 记录上一帧的轨道切线

    void Start()
    {
        initialSpline = railSpline;
        // 初始化切线
        if (railSpline != null)
            lastTangent = (Vector3)railSpline.EvaluateTangent(0);
    }

    void Update()
    {
        if (railSpline == null) return;
        if (cooldownTimer > 0) cooldownTimer -= Time.deltaTime;

        float splineLength = railSpline.CalculateLength();
        currentSpeed = Mathf.Lerp(currentSpeed, 0, friction * Time.deltaTime);

        if (Mathf.Abs(currentSpeed) > 0.01f)
        {
            distanceTraveled += (currentSpeed * moveDirection) * Time.deltaTime;
        }

        // --- 起点重置逻辑 ---
        if (railSpline == initialSpline && distanceTraveled <= 0.3f)
        {
            if (moveDirection == -1)
            {
                moveDirection = 1;
                distanceTraveled = 0;
                currentSpeed = 0;
                lastSpline = null;
                Debug.Log("<color=orange>【重置】回到起点</color>");
            }
        }

        // 边界换轨判定
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

        UpdateCartPositionAndRotation(splineLength);
    }

    private void UpdateCartPositionAndRotation(float splineLength)
    {
        float normalizedPos = Mathf.Clamp01(distanceTraveled / splineLength);
        transform.position = railSpline.EvaluatePosition(normalizedPos);

        Vector3 currentTangent = (Vector3)railSpline.EvaluateTangent(normalizedPos);

        if (currentTangent != Vector3.zero && lastTangent != Vector3.zero)
        {
            Vector3 lastMovementDir = lastTangent * moveDirection;
            Vector3 currentMovementDir = currentTangent * moveDirection;

            // 1. 计算原始物理增量
            Quaternion deltaRotation = Quaternion.FromToRotation(lastMovementDir, currentMovementDir);

            // 2. --- 核心修改：扩大偏转幅度 ---
            // 通过 Lerp 从 identity 到 deltaRotation 并设置大于 1 的系数（例如 1.5f）
            // 1.0f 是 1:1 还原轨道偏转，1.5f 会夸大 50% 的偏转感
            float exaggerationMultiplier = 1.2f;
            Quaternion exaggeratedDelta = Quaternion.LerpUnclamped(Quaternion.identity, deltaRotation, exaggerationMultiplier);

            // 3. 应用夸大的增量
            // 使用较高的步进速度，让小车“追赶”这个夸大的目标
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                exaggeratedDelta * transform.rotation,
                rotationSmoothTime * Time.deltaTime * 150f // 提高追赶速度以配合扩大的偏转
            );
        }

        lastTangent = currentTangent;
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
                    PerformSwitch(nextSpline, 0.01f, 1);
                    return true;
                }
                else if (Vector3.Distance(transform.position, endPos) < detectionRadius)
                {
                    PerformSwitch(nextSpline, nextLength - 0.01f, -1);
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

        // 【关键点】换轨时，更新 lastTangent 为新轨道的起始切线
        // 这样下一帧计算 deltaRotation 时，是基于新轨道的方向开始算的，不会出现 180 度跳变
        lastTangent = (Vector3)railSpline.EvaluateTangent(newDist / railSpline.CalculateLength());

        currentSpeed = 10f;
        Debug.Log($"<color=lime>换轨成功: {next.name}</color>");
    }

    public void AddWindForce(float force)
    {
        currentSpeed += force;
        currentSpeed = Mathf.Clamp(currentSpeed, -maxSpeed, maxSpeed);
    }
}