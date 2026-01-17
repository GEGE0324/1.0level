using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;
using UnityEngine.AI; // 必须引用，用于处理 Agent 停止
using Opsive.UltimateCharacterController.Character;
using Opsive.UltimateCharacterController.Character.Abilities.Items;

[TaskCategory("UCC Custom")]
public class UCCAimControl : Action
{
    public bool stopAim = false;
    private UltimateCharacterLocomotion locomotion;
    private Aim aimAbility;
    private NavMeshAgent agent;

    public override void OnAwake()
    {
        locomotion = GetComponent<UltimateCharacterLocomotion>();
        aimAbility = locomotion.GetAbility<Aim>();
        agent = GetComponent<NavMeshAgent>();
    }

    public override TaskStatus OnUpdate()
    {
        if (aimAbility == null) return TaskStatus.Failure;

        if (stopAim)
        {
            if (aimAbility.IsActive)
            {
                locomotion.TryStopAbility(aimAbility);
            }

            // 恢复寻路（如果需要的话，视你的行为树逻辑而定）
            if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
            {
                agent.isStopped = false;
            }
        }
        else
        {
            // --- 核心修复：锁定移动 ---
            // 瞄准期间，强制清空 UCC 的马达输入，杜绝平移滑行
            locomotion.RawInputVector = Vector3.zero;

            // --- 核心修复：停止寻路代理 ---
            // 确保 Agent 不会在瞄准时尝试计算位移
            if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
            }

            // 如果已经在瞄准了，就不再重复启动，防止每帧重置
            if (!aimAbility.IsActive)
            {
                locomotion.TryStartAbility(aimAbility);
            }
        }

        return TaskStatus.Success;
    }
}