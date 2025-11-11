using UnityEngine;
using UnityEngine.AI;
using System.Collections;

/// <summary>
/// Base class for all enemy types, providing common functionality
/// </summary>
public abstract class BaseEnemy : MonoBehaviour
{
    [Header("Base Enemy Settings")]
    public float maxHealth = 100f;
    [HideInInspector] public float health;
    public float moveSpeed = 3.5f;
    public float detectionRange = 10f;
    public float attackRange = 2f;
    public float attackDamage = 25f;
    public float attackCooldown = 1.5f;
    public bool stopPushingAtAttackRange = true;

    public enum DeathBehavior { Instant, TimedDelay }
    [Header("Death Settings")]
    public DeathBehavior deathBehavior = DeathBehavior.TimedDelay;
    public float deathDestroyDelay = 0.5f;
    public bool hideVisualsOnDeath = true;
    
    [Header("Components")]
    public Transform player;
    public NavMeshAgent agent;
    public Animator animator;
    
    // Protected fields that derived classes can access
    protected float lastAttackTime;
    protected bool isDead = false;
    protected bool isAttacking = false;
    protected bool hasWalkParam = false;
    protected bool hasAttackParam = false;
    protected bool hasDeathParam = false;
    
    [Header("Animator Runtime Settings")]
    public bool forceAnimatorAlwaysAnimate = true;
    public bool disableAnimatorRootMotion = true;
    
    // Animation parameter names - can be overridden by derived classes
    protected virtual string WalkParam => "IsWalking";
    protected virtual string AttackParam => "Attack";
    protected virtual string DeathParam => "Death";
    
    // Virtual methods that can be overridden by derived classes
    protected virtual void Start()
    {
        InitializeEnemy();
    }
    
    protected virtual void Update()
    {
        if (isDead) return;
        
        if (player == null) return;
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        UpdateEnemyBehavior(distanceToPlayer);
    }
    
    protected virtual void InitializeEnemy()
    {
        // Initialize health
        health = Mathf.Clamp(maxHealth, 1f, Mathf.Infinity);
        
        // Find player if not assigned
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }
        
        // Get components if not assigned
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();
        if (animator == null)
            animator = GetComponent<Animator>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        SetupAnimator();
        SetupNavMeshAgent();
        SetupPhysics();
        SetupVerticalPlacement();
    }
    
    protected virtual void SetupAnimator()
    {
        if (animator == null)
        {
            Debug.LogWarning($"{GetType().Name}: No Animator found on '{name}' or its children. Animations will not play.");
            return;
        }

        // Ensure Animator is enabled
        if (!animator.enabled)
        {
            animator.enabled = true;
            Debug.LogWarning($"{GetType().Name}: Animator was disabled on '{name}', enabling it.");
        }
        
        // Always animate (important for runtime clones)
        if (forceAnimatorAlwaysAnimate)
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        
        // Disable root motion if needed
        if (disableAnimatorRootMotion)
            animator.applyRootMotion = false;
        
        animator.speed = GetAnimatorSpeed();
        if (animator.layerCount > 0)
            animator.SetLayerWeight(0, 1f);

        // Check Animator Controller assignment
        if (animator.runtimeAnimatorController == null)
        {
            Debug.LogWarning($"{GetType().Name}: Animator on '{name}' has no Controller assigned.");
        }
        else
        {
            var controllerName = animator.runtimeAnimatorController.name;
            Debug.Log($"{GetType().Name}: Animator='{animator.name}', Controller='{controllerName}' on '{name}'");
            
            // Force rebind for runtime clones
            animator.Rebind();
            animator.Update(0f);
            Debug.Log($"{GetType().Name}: Forced animator rebind on '{name}'");
            
            // Additional setup for spawned enemies
            StartCoroutine(DelayedAnimatorInitialization());
        }

        CacheAnimatorParameters();
    }
    
    protected virtual float GetAnimatorSpeed()
    {
        return 1f; // Default animator speed
    }
    
    protected virtual void CacheAnimatorParameters()
    {
        if (animator == null) return;
        
        hasWalkParam = AnimatorHasParameter(WalkParam, AnimatorControllerParameterType.Bool);
        hasAttackParam = AnimatorHasParameter(AttackParam, AnimatorControllerParameterType.Trigger);
        hasDeathParam = AnimatorHasParameter(DeathParam, AnimatorControllerParameterType.Trigger);

        if (!hasWalkParam)
            Debug.LogWarning($"{GetType().Name}: Animator missing Bool parameter '{WalkParam}' on '{name}'.");
        if (!hasAttackParam)
            Debug.LogWarning($"{GetType().Name}: Animator missing Trigger parameter '{AttackParam}' on '{name}'.");
        if (!hasDeathParam)
            Debug.LogWarning($"{GetType().Name}: Animator missing Trigger parameter '{DeathParam}' on '{name}'.");
    }
    
    protected virtual void SetupNavMeshAgent()
    {
        if (agent == null) return;
        
        agent.speed = moveSpeed;
        agent.stoppingDistance = Mathf.Max(0f, attackRange - 0.5f);
        agent.acceleration = GetAgentAcceleration();
        agent.angularSpeed = GetAgentAngularSpeed();
        agent.obstacleAvoidanceType = GetObstacleAvoidanceType();
    }
    
    protected virtual float GetAgentAcceleration()
    {
        return 8f; // Default acceleration
    }
    
    protected virtual float GetAgentAngularSpeed()
    {
        return 120f; // Default angular speed
    }
    
    protected virtual ObstacleAvoidanceType GetObstacleAvoidanceType()
    {
        return ObstacleAvoidanceType.HighQualityObstacleAvoidance;
    }
    
    protected virtual void SetupPhysics()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }
    
    protected virtual void SetupVerticalPlacement()
    {
        if (agent == null) return;
        
        var capsule = GetComponent<CapsuleCollider>();
        if (capsule != null)
        {
            agent.height = Mathf.Max(agent.height, capsule.height);
            agent.radius = Mathf.Max(agent.radius, capsule.radius);
            agent.baseOffset = Mathf.Max(0f, (capsule.height * 0.5f) - capsule.center.y);
        }

        // Snap onto NavMesh at start
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
        {
            Vector3 snapped = hit.position;
            snapped.y += agent.baseOffset;
            transform.position = snapped;
        }
    }
    
    protected virtual void UpdateEnemyBehavior(float distanceToPlayer)
    {
        // Check if player is in detection range
        if (distanceToPlayer <= detectionRange)
        {
            // Face the player
            FacePlayer();
            
            // Check if in attack range
            if (distanceToPlayer <= attackRange)
            {
                HandleAttackRange();
            }
            else
            {
                HandleMovementToPlayer();
            }
        }
        else
        {
            HandleOutOfRange();
        }
    }
    
    protected virtual void FacePlayer()
    {
        if (player == null) return;
        
        Vector3 lookDirection = (player.position - transform.position).normalized;
        lookDirection.y = 0;
        if (lookDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(lookDirection);
        }
    }
    
    protected virtual void HandleAttackRange()
    {
        // Stop moving
        if (agent != null && agent.isActiveAndEnabled)
        {
            if (stopPushingAtAttackRange)
            {
                agent.isStopped = true;
            }
            agent.ResetPath();
            agent.velocity = Vector3.zero;
            agent.SetDestination(transform.position);
        }
            
        if (animator != null && hasWalkParam)
            animator.SetBool(WalkParam, false);
        
        // Attack if cooldown is over
        if (Time.time - lastAttackTime >= attackCooldown && !isAttacking)
        {
            Attack();
        }
    }
    
    protected virtual void HandleMovementToPlayer()
    {
        // Move towards player
        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
            if (animator != null && hasWalkParam)
            {
                animator.SetBool(WalkParam, true);
            }
        }
    }
    
    protected virtual void HandleOutOfRange()
    {
        // Player not in range, stop moving
        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.isStopped = false;
            agent.SetDestination(transform.position);
            agent.velocity = Vector3.zero;
        }
        if (animator != null && hasWalkParam)
            animator.SetBool(WalkParam, false);
    }
    
    protected virtual void Attack()
    {
        isAttacking = true;
        lastAttackTime = Time.time;
        
        Debug.Log($"{GetType().Name}: Performing attack on '{name}'");
        
        // Trigger attack animation
        if (animator != null && hasAttackParam)
            animator.SetTrigger(AttackParam);
        
        // Deal damage to player
        DealDamageToPlayer();
        
        // Reset attack state
        Invoke(nameof(ResetAttack), GetAttackRecoveryTime());
    }
    
    protected virtual float GetAttackRecoveryTime()
    {
        return 1f; // Default recovery time
    }
    
    protected virtual void DealDamageToPlayer()
    {
        if (player == null) return;
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer <= attackRange)
        {
            var playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
                Debug.Log($"{GetType().Name} attacked player for {attackDamage} damage!");
            }
            else
            {
                Debug.LogWarning($"Player hit by {GetType().Name}! (No PlayerHealth component found on player)");
            }
        }
    }
    
    protected virtual void ResetAttack()
    {
        isAttacking = false;
    }
    
    public virtual void TakeDamage(float damage)
    {
        if (isDead) return;
        
        health -= damage;
        Debug.Log($"{GetType().Name} took {damage} damage. Health: {health:F1}/{maxHealth:F1}");
        
        if (health <= 0)
        {
            Debug.Log($"{GetType().Name} died after taking {damage} damage");
            Die();
        }
    }
    
    public virtual void Die()
    {
        isDead = true;
        
        // Stop movement
        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.velocity = Vector3.zero;
            agent.enabled = false;
        }
            
        // Trigger death animation
        if (animator != null && hasDeathParam)
            animator.SetTrigger(DeathParam);
        
        // Snap to ground
        StartCoroutine(SnapToGroundDelayed());
        
        // Disable collider
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = false;

        // Hide visuals after death animation
        if (hideVisualsOnDeath)
        {
            StartCoroutine(HideAfterDeathAnimation());
        }

        // Destroy behavior
        if (deathBehavior == DeathBehavior.Instant)
        {
            Destroy(gameObject);
        }
        else
        {
            Destroy(gameObject, Mathf.Max(0f, deathDestroyDelay));
        }

        Debug.Log($"{GetType().Name} died!");
    }
    
    protected virtual void OnDestroy()
    {
        if (Application.isPlaying)
        {
            EnemySpawnManager spawnManager = FindFirstObjectByType<EnemySpawnManager>();
            if (spawnManager != null)
            {
                spawnManager.RegisterDeath();
            }
            else
            {
                Debug.LogWarning($"{GetType().Name}: EnemySpawnManager not found when enemy died");
            }
        }
    }
    
    // Coroutines and helper methods
    protected virtual System.Collections.IEnumerator HideAfterDeathAnimation()
    {
        yield return new WaitForSeconds(GetDeathAnimationTime());
        
        var renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].enabled = false;
        }

        var canvases = GetComponentsInChildren<Canvas>(true);
        for (int i = 0; i < canvases.Length; i++)
        {
            canvases[i].enabled = false;
        }
    }
    
    protected virtual float GetDeathAnimationTime()
    {
        return 2f; // Default death animation time
    }
    
    protected virtual System.Collections.IEnumerator SnapToGroundDelayed()
    {
        yield return new WaitForSeconds(1f);
        StartCoroutine(SmoothSnapToGround());
    }
    
    protected virtual System.Collections.IEnumerator SmoothSnapToGround()
    {
        Vector3 startPosition = transform.position;
        Vector3 targetPosition = startPosition;
        
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out hit, 10f))
        {
            targetPosition.y = hit.point.y;
        }
        else if (NavMesh.SamplePosition(transform.position, out NavMeshHit navHit, 5f, NavMesh.AllAreas))
        {
            targetPosition.y = navHit.position.y;
        }
        else
        {
            yield break;
        }
        
        float duration = 0.5f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = t * t * (3f - 2f * t);
            
            transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }
        
        transform.position = targetPosition;
    }
    
    protected virtual System.Collections.IEnumerator DelayedAnimatorInitialization()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        
        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
            animator.Play("Armature|Idle", 0, 0f);
            
            Debug.Log($"{GetType().Name}: Delayed init - Testing animator parameters on '{name}'");
            if (hasWalkParam)
            {
                animator.SetBool(WalkParam, false);
                Debug.Log("  - Set IsWalking to false");
            }
            
            Debug.Log($"{GetType().Name}: Delayed animator initialization completed for '{name}'");
        }
    }
    
    // Utility method for animator parameter checking
    protected bool AnimatorHasParameter(string paramName, AnimatorControllerParameterType type)
    {
        if (animator == null || string.IsNullOrEmpty(paramName)) return false;
        var parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            var p = parameters[i];
            if (p.type == type && p.name == paramName)
                return true;
        }
        return false;
    }
    
    // Visual debugging
    protected virtual void OnDrawGizmosSelected()
    {
        // Draw detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        // Draw attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // Draw line to player if in range
        if (player != null)
        {
            float distance = Vector3.Distance(transform.position, player.position);
            if (distance <= detectionRange)
            {
                Gizmos.color = distance <= attackRange ? Color.red : Color.yellow;
                Gizmos.DrawLine(transform.position, player.position);
            }
        }
    }
}