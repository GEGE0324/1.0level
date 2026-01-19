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
            // 🔒 当 Agent 被停止时 (例如瞄准状态)，不要继续移动
            // 这样可以防止敌人瞄准玩家时自己的位置发生漂移
            if (m_NavMeshAgent.isStopped)
            {
                // 只同步位置，不产生移动
                m_CharacterLocomotion.Move(0, 0, 0);
                m_NavMeshAgent.nextPosition = transform.position;
                return;
            }

            // 获取一帧的期望方向
            Vector3 desiredVelocity = m_NavMeshAgent.desiredVelocity;
            Vector3 input = transform.InverseTransformDirection(desiredVelocity);

            // 使用 Move 让 Opsive 接管实际位移
            // 数值可以调整一下，防止数值太大瞬间弹开
            m_CharacterLocomotion.Move(Mathf.Clamp(input.x, -1, 1), Mathf.Clamp(input.z, -1, 1), 0);

            // 关键：手动同步 Agent 的逻辑回到角色的真实位置
            // 这会让 Agent 的导航逻辑对角色模型为中心，避免冲突
            m_NavMeshAgent.nextPosition = transform.position;
        }
    }
}
