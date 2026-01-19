using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;
using UnityEngine.AI;
using Opsive.UltimateCharacterController.Character;
using Opsive.UltimateCharacterController.Character.Abilities.Items;
using Opsive.UltimateCharacterController.Character.Abilities.AI;

[TaskCategory("UCC Custom")]
[TaskDescription("Fires the equipped weapon (e.g., bow/arrow) at the target.")]
public class UCCUseControl : Action
{
    public SharedTransform target;
    
    // Which slot index to use (0 = primary weapon)
    public int slotIndex = 0;

    private UltimateCharacterLocomotion locomotion;
    private Use useAbility;
    private NavMeshAgent agent;
    private NavMeshAgentMovement navMeshAbility;

    private bool attackStarted;
    private float attackStartTime;
    private float maxAttackDuration = 3f; // Timeout safety

    public override void OnAwake()
    {
        locomotion = GetComponent<UltimateCharacterLocomotion>();
        useAbility = locomotion.GetAbility<Use>();
        navMeshAbility = locomotion.GetAbility<NavMeshAgentMovement>();
        agent = GetComponent<NavMeshAgent>();
    }

    public override void OnStart()
    {
        attackStarted = false;
        attackStartTime = Time.time;
        
        // Stop all movement
        StopAllMovement();
        
        // Face the target
        FaceTarget();
        
        // Fire the weapon
        StartAttack();
    }

    public override TaskStatus OnUpdate()
    {
        if (useAbility == null) return TaskStatus.Failure;

        // Maintain stopped state
        MaintainStoppedState();
        
        // Keep facing target while attacking
        FaceTarget();

        // Timeout safety - don't get stuck
        if (Time.time - attackStartTime > maxAttackDuration)
        {
            Debug.Log("UCCUseControl: Attack timeout, returning Success.");
            return TaskStatus.Success;
        }

        // Check if attack has completed
        if (attackStarted && !useAbility.IsActive)
        {
            Debug.Log("UCCUseControl: Arrow fired successfully!");
            return TaskStatus.Success;
        }

        return TaskStatus.Running;
    }

    private void FaceTarget()
    {
        if (target.Value == null) return;
        
        Vector3 directionToTarget = target.Value.position - transform.position;
        directionToTarget.y = 0; // Keep on horizontal plane
        
        if (directionToTarget.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
        }
    }

    private void StopAllMovement()
    {
        locomotion.RawInputVector = Vector3.zero;

        if (navMeshAbility != null && navMeshAbility.IsActive)
        {
            locomotion.TryStopAbility(navMeshAbility);
        }

        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.updateRotation = false;
            
            // Prevent NavMeshAgent from updating Transform position (fixes snap/glitch)
            agent.updatePosition = false;
            
            agent.velocity = Vector3.zero;
            agent.ResetPath();
        }
    }

    private void MaintainStoppedState()
    {
        locomotion.RawInputVector = Vector3.zero;
        
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.velocity = Vector3.zero;
            // Double check to ensure position update stays off
            agent.updatePosition = false; 
        }
    }

    private void StartAttack()
    {
        if (!attackStarted)
        {
            useAbility.AbilityIndexParameter = slotIndex;
            
            if (locomotion.TryStartAbility(useAbility))
            {
                attackStarted = true;
                Debug.Log("UCCUseControl: Firing arrow...");
            }
            else
            {
                Debug.LogWarning("UCCUseControl: Failed to fire arrow.");
            }
        }
    }

    public override void OnEnd()
    {
        if (useAbility != null && useAbility.IsActive)
        {
            locomotion.TryStopAbility(useAbility);
        }
        
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.velocity = Vector3.zero;
            
            // 重要: 在恢复控制前，将 Agent 瞬间移动到当前角色位置
            // 这防止了角色被"拉回"到 Agent 之前的滞留位置
            agent.Warp(transform.position);
            
            // Restore NavMeshAgent control
            agent.updatePosition = true;
            agent.updateRotation = true;
            agent.isStopped = false;
        }
    }
}
