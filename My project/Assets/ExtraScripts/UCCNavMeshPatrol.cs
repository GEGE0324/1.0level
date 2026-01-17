using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Opsive.UltimateCharacterController.Character;
using Opsive.UltimateCharacterController.Character.Abilities;
using Opsive.UltimateCharacterController.Character.Abilities.AI;
using UnityEngine;
// 解决命名空间冲突
using Tooltip = BehaviorDesigner.Runtime.Tasks.TooltipAttribute;

[TaskCategory("UCC Custom")]
[TaskDescription("让UCC角色在路径点之间巡逻并自动播放动画")]
public class UCCNavMeshPatrol : Action
{
    [Tooltip("存放路径点的列表")]
    public SharedGameObjectList waypoints;
    [Tooltip("到达每个点后的等待时间")]
    public SharedFloat waitTime = 0;

    private int waypointIndex = 0;
    private float arrivalTime = -1;
    private UltimateCharacterLocomotion locomotion;
    private NavMeshAgentMovement navMeshAbility;

    public override void OnAwake()
    {
        locomotion = GetComponent<UltimateCharacterLocomotion>();
        if (locomotion != null)
        {
            navMeshAbility = locomotion.GetAbility<NavMeshAgentMovement>();
        }
    }

    public override void OnStart()
    {
        if (waypoints.Value == null || waypoints.Value.Count == 0) return;

        if (navMeshAbility != null)
        {
            locomotion.TryStartAbility(navMeshAbility);
            SetNextDestination();
        }
    }

    public override TaskStatus OnUpdate()
    {
        if (navMeshAbility == null || waypoints.Value == null || waypoints.Value.Count == 0)
            return TaskStatus.Failure;

        if (navMeshAbility.HasArrived)
        {
            // 开始计时等待
            if (arrivalTime == -1) arrivalTime = Time.time;

            if (Time.time - arrivalTime >= waitTime.Value)
            {
                // 等待结束，前往下一个点
                arrivalTime = -1;
                waypointIndex = (waypointIndex + 1) % waypoints.Value.Count;
                SetNextDestination();
            }
        }

        return TaskStatus.Running;
    }

    private void SetNextDestination()
    {
        var targetObj = waypoints.Value[waypointIndex];
        if (targetObj != null)
        {
            navMeshAbility.SetDestination(targetObj.transform.position);
        }
    }

    public override void OnEnd()
    {
        if (navMeshAbility != null && navMeshAbility.IsActive)
        {
            locomotion.TryStopAbility(navMeshAbility);
        }
        arrivalTime = -1;
    }
}