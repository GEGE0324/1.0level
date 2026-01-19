using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Opsive.UltimateCharacterController.Character;
using Opsive.UltimateCharacterController.Character.Abilities.AI;
using UnityEngine;
using UnityEngine.AI;

[TaskCategory("UCC Custom")]
[TaskDescription("Moves to a random position within the attack range around the target. Returns Success when arrived.")]
public class UCCMoveToRandomPosition : Action
{
   
    public SharedTransform target;
    
    
    public float minRadius = 3f;
    
   
    public float maxRadius = 8f;
    
   
    public float arrivalThreshold = 0.5f;
    
    // If player is beyond this distance, return Failure (so AI can chase)
    public float alertRange = 15f;

    private UltimateCharacterLocomotion locomotion;
    private NavMeshAgentMovement navMeshAbility;
    private NavMeshAgent agent;
    private Vector3 targetPosition;
    private bool hasDestination;

    public override void OnAwake()
    {
        locomotion = GetComponent<UltimateCharacterLocomotion>();
        navMeshAbility = locomotion.GetAbility<NavMeshAgentMovement>();
        agent = GetComponent<NavMeshAgent>();
    }

    public override void OnStart()
    {
        hasDestination = false;
        
        if (target.Value == null || navMeshAbility == null)
        {
            return;
        }

        // Calculate a random position around the target
        targetPosition = GetRandomPositionAroundTarget();
        
        if (targetPosition != Vector3.zero)
        {
            // Start NavMesh movement ability
            if (!navMeshAbility.IsActive)
            {
                locomotion.TryStartAbility(navMeshAbility);
            }
            
            navMeshAbility.SetDestination(targetPosition);
            hasDestination = true;
            Debug.Log($"UCCMoveToRandomPosition: Moving to {targetPosition}");
        }
    }

    public override TaskStatus OnUpdate()
    {
        if (target.Value == null || navMeshAbility == null)
        {
            return TaskStatus.Failure;
        }

        if (!hasDestination)
        {
            return TaskStatus.Failure;
        }

        // Check if target (player) is outside alert range
        float distanceToTarget = Vector3.Distance(transform.position, target.Value.position);
        if (distanceToTarget > alertRange)
        {
            Debug.Log($"UCCMoveToRandomPosition: Player outside alert range ({distanceToTarget:F2}), returning Failure.");
            return TaskStatus.Failure;
        }

        // Check if we've arrived at the random position
        float distanceToDestination = Vector3.Distance(transform.position, targetPosition);
        
        if (distanceToDestination <= arrivalThreshold || navMeshAbility.HasArrived)
        {
            Debug.Log("UCCMoveToRandomPosition: Arrived at position.");
            return TaskStatus.Success;
        }

        // Keep the navmesh ability active
        if (!navMeshAbility.IsActive)
        {
            locomotion.TryStartAbility(navMeshAbility);
            navMeshAbility.SetDestination(targetPosition);
        }

        return TaskStatus.Running;
    }

    private Vector3 GetRandomPositionAroundTarget()
    {
        if (target.Value == null) return Vector3.zero;

        // Try several times to find a valid NavMesh position
        for (int i = 0; i < 10; i++)
        {
            // Random angle around the target
            float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            
            // Random distance between min and max radius
            float randomDistance = Random.Range(minRadius, maxRadius);
            
            // Calculate position
            Vector3 offset = new Vector3(
                Mathf.Cos(randomAngle) * randomDistance,
                0f,
                Mathf.Sin(randomAngle) * randomDistance
            );
            
            Vector3 potentialPosition = target.Value.position + offset;
            
            // Check if this position is on the NavMesh
            NavMeshHit hit;
            if (NavMesh.SamplePosition(potentialPosition, out hit, 2f, NavMesh.AllAreas))
            {
                return hit.position;
            }
        }

        // Fallback: return current position (don't move)
        Debug.LogWarning("UCCMoveToRandomPosition: Could not find valid NavMesh position.");
        return transform.position;
    }

    public override void OnEnd()
    {
        if (navMeshAbility != null && navMeshAbility.IsActive)
        {
            locomotion.TryStopAbility(navMeshAbility);
        }
    }
}
