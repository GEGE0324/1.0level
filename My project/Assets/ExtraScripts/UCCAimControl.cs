using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;
using UnityEngine.AI;
using Opsive.UltimateCharacterController.Character;
using Opsive.UltimateCharacterController.Character.Abilities.Items;
using Opsive.UltimateCharacterController.Character.Abilities.AI;

[TaskCategory("UCC Custom")]
[TaskDescription("Aims at target, then manually spawns a projectile (bypassing Use ability/animation) to avoid root motion displacement.")]
public class UCCAimControl : Action
{
    public SharedTransform target;
    
    // How long to aim before attacking
    public float aimDuration = 2f;
    
    // How fast the aim direction tracks the player
    public float trackingSpeed = 2f;
    
    // Manual Projectile Settings
    
    public GameObject projectilePrefab;
    
    public Transform firePoint;
    public float projectileSpeed = 40f;
    public float projectileDamage = 10f; // Optional, requires custom damage script or UCC Projectile component

    private UltimateCharacterLocomotion locomotion;
    private Aim aimAbility;
    private NavMeshAgent agent;
    private NavMeshAgentMovement navMeshAbility;
    
    private bool isAiming;
    private float aimStartTime;
    private Vector3 initialPosition;
    private Vector3 currentAimDirection;
    private bool hasFired;

    public override void OnAwake()
    {
        locomotion = GetComponent<UltimateCharacterLocomotion>();
        aimAbility = locomotion.GetAbility<Aim>();
        navMeshAbility = locomotion.GetAbility<NavMeshAgentMovement>();
        agent = GetComponent<NavMeshAgent>();
        
        if (locomotion == null) Debug.LogError("UCCAimControl: Locomotion is NULL");
        if (aimAbility == null) Debug.LogError("UCCAimControl: Aim Ability is NULL");
    }

    public override void OnStart()
    {
        isAiming = false;
        hasFired = false;
        aimStartTime = Time.time;
        
        // Cache initial position to prevent drift
        initialPosition = transform.position;

        // Initialize aim direction
        if (target.Value != null)
        {
            currentAimDirection = (target.Value.position - transform.position).normalized;
            // Immediate orientation towards target to satisfy "turn before aim"
            Vector3 flatDir = new Vector3(currentAimDirection.x, 0, currentAimDirection.z).normalized;
            if (flatDir.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(flatDir);
            }
        }
        else
        {
            currentAimDirection = transform.forward;
        }
        
        StartAim();
    }

    public override TaskStatus OnUpdate()
    {
        if (aimAbility == null) return TaskStatus.Failure;

        // Force lock position (to combat root motion since Locker is gone)
        transform.position = initialPosition;

        // Phase 1: Aiming & Tracking
        if (!hasFired)
        {
            float elapsedTime = Time.time - aimStartTime;
            
            if (elapsedTime < aimDuration)
            {
                UpdateAimTracking();
                MaintainStoppedState();
                RotateTowardsAimDirection();
                return TaskStatus.Running;
            }
            else
            {
                // Aim duration finished -> MANUAL FIRE
                ManualFire();
                hasFired = true;
                
                StopAim();
                return TaskStatus.Success;
            }
        }

        return TaskStatus.Success;
    }

    private void UpdateAimTracking()
    {
        if (target.Value == null) return;

        Vector3 desiredDirection = (target.Value.position - transform.position).normalized;
        currentAimDirection = Vector3.Lerp(currentAimDirection, desiredDirection, trackingSpeed * Time.deltaTime).normalized;
    }

    private void RotateTowardsAimDirection()
    {
        if (currentAimDirection == Vector3.zero) return;
        
        Vector3 flatDirection = new Vector3(currentAimDirection.x, 0, currentAimDirection.z).normalized;
        if (flatDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(flatDirection);
            // Directly update rotation (UCC might fight this without Locker, but we strive to keep it stationary)
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, trackingSpeed * Time.deltaTime);
        }
    }

    private void StartAim()
    {
        if (locomotion == null) return;
        locomotion.RawInputVector = Vector3.zero;

        if (navMeshAbility != null && navMeshAbility.IsActive) locomotion.TryStopAbility(navMeshAbility);

        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.updateRotation = false; // Disable agent rotation to allow manual control
            agent.updatePosition = false; 
            agent.velocity = Vector3.zero;
            agent.ResetPath();
        }

        if (!isAiming)
        {
            if (locomotion.TryStartAbility(aimAbility))
            {
                isAiming = true;
            }
        }
    }

    private void ManualFire()
    {
        if (projectilePrefab == null)
        {
            Debug.LogError("UCCAimControl: Projectile Prefab is not assigned!");
            return;
        }

        Vector3 spawnPos;
        if (firePoint != null)
        {
            spawnPos = firePoint.position;
        }
        else
        {
            spawnPos = transform.position + Vector3.up * 1.5f + transform.forward * 1.0f;
        }

        if (currentAimDirection == Vector3.zero) currentAimDirection = transform.forward;
        Quaternion spawnRot = Quaternion.LookRotation(currentAimDirection);

        // --- FIXED: Use Opsive ObjectPool to avoid "Unable to pool" error ---
        // Directly use ObjectPool.Instantiate as IsObjectPoolActive was causing errors
        GameObject proj = Opsive.Shared.Game.ObjectPool.Instantiate(projectilePrefab, spawnPos, spawnRot);
        // --------------------------------------------------------------------
        
        // 1. Try to use UCC Projectile initialization (Best way for UCC)
        var uccProjectile = proj.GetComponent<Opsive.UltimateCharacterController.Objects.Projectile>();
        if (uccProjectile != null)
        {
            Debug.Log("UCCAimControl: UCC Projectile detected. Initializing via UCC API.");
            // --- FIXED: Pass the default damage data from the prefab instead of null ---
            var damageData = uccProjectile.DefaultImpactDamageData;
            uccProjectile.Initialize(0, currentAimDirection * projectileSpeed, Vector3.zero, gameObject, damageData);
            return; 
        }

        // 2. Fallback to standard Rigidbody if it's NOT a UCC projectile
        Rigidbody rb = proj.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = proj.AddComponent<Rigidbody>();
            rb.useGravity = false;
        }

        if (rb != null)
        {
            rb.isKinematic = false;

    #if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = currentAimDirection * projectileSpeed;
    #else
            rb.linearVelocity = currentAimDirection * projectileSpeed;
    #endif
            Debug.Log($"UCCAimControl: Manual Fire (Rigidbody fallback) to {currentAimDirection} with speed {projectileSpeed}");
        }
    }

    private void MaintainStoppedState()
    {
        locomotion.RawInputVector = Vector3.zero;
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.velocity = Vector3.zero;
            agent.updatePosition = false;
        }
    }

    private void StopAim()
    {
        if (isAiming)
        {
            locomotion.TryStopAbility(aimAbility);
            isAiming = false;
        }
        
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.velocity = Vector3.zero;
            agent.Warp(initialPosition);
            
            agent.isStopped = false;
            agent.updateRotation = true;
            agent.updatePosition = true;
        }
    }

    public override void OnEnd()
    {
        StopAim();
    }
}

