using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;
using UnityEngine.AI; // 必须引用，用于修复 NavMeshAgent 报错
using Opsive.UltimateCharacterController.Character;
using Opsive.UltimateCharacterController.Character.Abilities.Items;

[TaskCategory("UCC Custom")]
public class UCCUseControl : Action
{
    public bool stopUse = false;
    private UltimateCharacterLocomotion locomotion;
    private Use useAbility;
    private NavMeshAgent agent;

    public override void OnAwake()
    {
        locomotion = GetComponent<UltimateCharacterLocomotion>();
        useAbility = locomotion.GetAbility<Use>();
        agent = GetComponent<NavMeshAgent>();
    }

    public override TaskStatus OnUpdate()
    {
        if (useAbility == null) return TaskStatus.Failure;

        if (stopUse)
        {
            if (useAbility.IsActive)
            {
                locomotion.TryStopAbility(useAbility);
            }
        }
        else
        {
            // --- 核心修复：锁定移动 (使用 RawInputVector) ---
            // 这会强制清空 UCC 的马达输入，防止拉弓时平移
            locomotion.RawInputVector = Vector3.zero;

            // --- 核心修复：安全停止 NavMeshAgent ---
            // 解决你看到的 "Stop can only be called on an active agent" 报错
            if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
            }

            if (!useAbility.IsActive)
            {
                useAbility.AbilityIndexParameter = 0; // 对应弓箭 ID
                locomotion.TryStartAbility(useAbility);
            }
        }
        return TaskStatus.Success;
    }
}