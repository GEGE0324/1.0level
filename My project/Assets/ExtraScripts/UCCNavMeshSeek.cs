using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Opsive.UltimateCharacterController.Character;
using Opsive.UltimateCharacterController.Character.Abilities;
using Opsive.UltimateCharacterController.Character.Abilities.AI;
using UnityEngine;

[TaskCategory("UCC Custom")]
public class UCCNavMeshSeek : Action
{
    public SharedTransform target;

    private UltimateCharacterLocomotion locomotion;
    private NavMeshAgentMovement navMeshAbility;

    public override void OnAwake()
    {
        locomotion = GetComponent<UltimateCharacterLocomotion>();
        navMeshAbility = locomotion.GetAbility<NavMeshAgentMovement>();
    }

    public override void OnStart()
    {
        if (navMeshAbility != null && target.Value != null)
        {
            // 确保导航能力处于激活状态
            if (!navMeshAbility.IsActive)
            {
                locomotion.TryStartAbility(navMeshAbility);
            }
            navMeshAbility.SetDestination(target.Value.position);
        }
    }

    public override TaskStatus OnUpdate()
    {
        if (navMeshAbility == null || target.Value == null) return TaskStatus.Failure;

        // 核心：每一帧同步玩家位置，实现动态追踪
        navMeshAbility.SetDestination(target.Value.position);

        // 如果能力被其他动作意外打断，尝试重新启动
        if (!navMeshAbility.IsActive)
        {
            locomotion.TryStartAbility(navMeshAbility);
        }

        // 到达判定：根据 NavMeshAgent 的 Stopping Distance 决定
        if (navMeshAbility.HasArrived)
        {
            return TaskStatus.Success;
        }

        return TaskStatus.Running;
    }

    public override void OnEnd()
    {
        if (navMeshAbility != null && navMeshAbility.IsActive)
        {
            locomotion.TryStopAbility(navMeshAbility);
        }
    }
}