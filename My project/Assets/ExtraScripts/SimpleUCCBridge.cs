using UnityEngine;
using UnityEngine.AI;
using Opsive.UltimateCharacterController.Character; // 必须引用这个

public class SimpleUCCBridge : MonoBehaviour
{
    private NavMeshAgent m_NavMeshAgent;
    private UltimateCharacterLocomotion m_CharacterLocomotion;

    void Start()
    {
        m_NavMeshAgent = GetComponent<NavMeshAgent>();
        m_CharacterLocomotion = GetComponent<UltimateCharacterLocomotion>();

        // 关键：禁止 Agent 直接搬动模型
        m_NavMeshAgent.updatePosition = false;
        // 禁止 Agent 自动旋转（如果抖动包含旋转方向，请也设为 false）
        m_NavMeshAgent.updateRotation = false;
    }

    void Update()
    {
        if (m_NavMeshAgent.isActiveAndEnabled && m_NavMeshAgent.isOnNavMesh)
        {
            // 计算下一帧的输入方向
            Vector3 desiredVelocity = m_NavMeshAgent.desiredVelocity;
            Vector3 input = transform.InverseTransformDirection(desiredVelocity);

            // 使用 Move 方法让 Opsive 的物理引擎执行真实位移
            // 建议给输入值加一个限制，防止数值过大导致瞬间弹射
            m_CharacterLocomotion.Move(Mathf.Clamp(input.x, -1, 1), Mathf.Clamp(input.z, -1, 1), 0);

            // 核心：手动将 Agent 的逻辑中心拉回到角色当前的真实位置
            // 这样 Agent 的路径计算就会以角色模型为起点，消除抖动
            m_NavMeshAgent.nextPosition = transform.position;
        }
    }
}
