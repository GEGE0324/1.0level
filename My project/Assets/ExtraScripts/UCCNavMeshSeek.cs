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
            Debug.Log($"UCCNavMeshSeek: OnStart. Target: {target.Value.position}");
            // 确保激活状态
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

        // 核心：每一帧同步目标，实现动态追击
        navMeshAbility.SetDestination(target.Value.position);

        // 保底
        if (!navMeshAbility.IsActive)
        {
            locomotion.TryStartAbility(navMeshAbility);
        }

        // 判断 NavMeshAgent 的 Stopping Distance 到达
        if (navMeshAbility.HasArrived)
        {
            Debug.Log("UCCNavMeshSeek: Arrived at target.");
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