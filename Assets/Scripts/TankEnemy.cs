using UnityEngine;
using UnityEngine.AI;
using System.Collections;

/// <summary>
/// Tank Enemy Archetype - High health, slow movement, high damage, longer range
/// </summary>
public class TankEnemy : MonoBehaviour
{
    [Header("Tank Enemy Settings")]
    public float maxHealth = 250f;  // Much higher health than SimpleEnemy
    public float health = 250f;
    public float moveSpeed = 1.5f;  // Slower than SimpleEnemy
    public float detectionRange = 15f;  // Longer detection range
    public float attackRange = 4f;  // Longer attack range
    public float attackDamage = 50f;  // Higher damage
    public float attackCooldown = 2.5f;  // Slower attacks
    public bool stopPushingAtAttackRange = true;

    [Header("Tank Special Abilities")]
    public float chargeSpeed = 6f;  // Speed during charge attack
    public float chargeDistance = 8f;  // Maximum charge distance
    public float chargeDamage = 75f;  // Damage during charge
    public float chargeRecoveryTime = 3f;  // Time to recover after charge
    public bool canCharge = true;  // Whether this tank can charge

    public enum DeathBehavior { Instant, TimedDelay }
    [Header("Death Settings")]
    public DeathBehavior deathBehavior = DeathBehavior.TimedDelay;
    public float deathDestroyDelay = 1f;  // Longer delay for tank death
    public bool hideVisualsOnDeath = true;
    
    [Header("Components")]
    public Transform player;
    public NavMeshAgent agent;
    public Animator animator;
    
    private float lastAttackTime;
    private float lastChargeTime;
    private bool isDead = false;
    private bool isAttacking = false;
    private bool isCharging = false;
    private bool isRecovering = false;
    private bool hasWalkParam = false;
    private bool hasAttackParam = false;
    private bool hasDeathParam = false;
    private bool hasChargeParam = false;
    
    [Header("Animator Runtime Settings")]
    public bool forceAnimatorAlwaysAnimate = true;
    public bool disableAnimatorRootMotion = true;
    
    // Animation parameter names
    private const string WALK_PARAM = "IsWalking";
    private const string ATTACK_PARAM = "Attack";
    private const string DEATH_PARAM = "Death";
    private const string CHARGE_PARAM = "Charge";
    
    void Start()
    {
        // Initialize health from maxHealth at start
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

        // --- RUNTIME ANIMATOR CHECKS FOR CLONES ---
        if (animator == null)
        {
            Debug.LogWarning($"TankEnemy: No Animator found on '{name}' or its children. Animations will not play.");
        }
        else
        {
            // Ensure Animator is enabled
            if (!animator.enabled)
            {
                animator.enabled = true;
                Debug.LogWarning($"TankEnemy: Animator was disabled on '{name}', enabling it.");
            }
            // Always animate (important for runtime clones)
            if (forceAnimatorAlwaysAnimate)
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            // Disable root motion if needed
            if (disableAnimatorRootMotion)
                animator.applyRootMotion = false;
            animator.speed = 1f;
            if (animator.layerCount > 0)
                animator.SetLayerWeight(0, 1f);

            // Check Animator Controller assignment
            if (animator.runtimeAnimatorController == null)
            {
                Debug.LogWarning($"TankEnemy: Animator on '{name}' has no Controller assigned.");
            }
            else
            {
                var controllerName = animator.runtimeAnimatorController.name;
                Debug.Log($"TankEnemy: Animator='{animator.name}', Controller='{controllerName}' on '{name}'");
                // Force rebind for runtime clones
                animator.Rebind();
                animator.Update(0f);
                Debug.Log($"TankEnemy: Forced animator rebind on '{name}'");
                // Additional setup for spawned enemies
                StartCoroutine(DelayedAnimatorInitialization());
            }

            // Cache animator parameter availability
            hasWalkParam = TankEnemyAnimatorExtensions.AnimatorHasParameter(animator, WALK_PARAM, AnimatorControllerParameterType.Bool);
            hasAttackParam = TankEnemyAnimatorExtensions.AnimatorHasParameter(animator, ATTACK_PARAM, AnimatorControllerParameterType.Trigger);
            hasDeathParam = TankEnemyAnimatorExtensions.AnimatorHasParameter(animator, DEATH_PARAM, AnimatorControllerParameterType.Trigger);
            hasChargeParam = TankEnemyAnimatorExtensions.AnimatorHasParameter(animator, CHARGE_PARAM, AnimatorControllerParameterType.Trigger);

            if (!hasWalkParam)
                Debug.LogWarning($"TankEnemy: Animator missing Bool parameter '{WALK_PARAM}' on '{name}'.");
            if (!hasAttackParam)
                Debug.LogWarning($"TankEnemy: Animator missing Trigger parameter '{ATTACK_PARAM}' on '{name}'.");
            if (!hasDeathParam)
                Debug.LogWarning($"TankEnemy: Animator missing Trigger parameter '{DEATH_PARAM}' on '{name}'.");
            if (!hasChargeParam && canCharge)
                Debug.LogWarning($"TankEnemy: Animator missing Trigger parameter '{CHARGE_PARAM}' on '{name}'. Charge attacks disabled.");
        }
            
        // Configure NavMesh Agent for tank behavior
        if (agent != null)
        {
            agent.speed = moveSpeed;
            agent.stoppingDistance = Mathf.Max(0f, attackRange - 0.5f);
            agent.acceleration = 4f; // Slower acceleration for heavy tank
            agent.angularSpeed = 60f; // Slower turning for tank
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        }
        
        // Configure Rigidbody to prevent falling
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // Calibrate agent vertical placement
        if (agent != null)
        {
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
    }
    
    void Update()
    {
        if (isDead || isRecovering) return;
        
        if (player == null) return;
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        // Check if player is in detection range
        if (distanceToPlayer <= detectionRange)
        {
            // Face the player
            Vector3 lookDirection = (player.position - transform.position).normalized;
            lookDirection.y = 0;
            if (lookDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(lookDirection);
            }
            
            // Check for charge attack (if tank can charge and conditions are met)
            bool shouldCharge = canCharge && 
                               !isCharging && 
                               distanceToPlayer > attackRange && 
                               distanceToPlayer <= chargeDistance &&
                               Time.time - lastChargeTime >= chargeRecoveryTime * 2f; // Longer cooldown for charge
            
            if (shouldCharge)
            {
                StartCoroutine(PerformChargeAttack());
                return;
            }
            
            // Check if in attack range
            if (distanceToPlayer <= attackRange && !isCharging)
            {
                // Stop moving and attack
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
                    animator.SetBool(WALK_PARAM, false);
                
                // Attack if cooldown is over
                if (Time.time - lastAttackTime >= attackCooldown && !isAttacking)
                {
                    Attack();
                }
            }
            else if (!isCharging)
            {
                // Move towards player
                if (agent != null && agent.isActiveAndEnabled)
                {
                    agent.isStopped = false;
                    agent.SetDestination(player.position);
                    if (animator != null && hasWalkParam)
                    {
                        animator.SetBool(WALK_PARAM, true);
                        Debug.Log($"TankEnemy: Set {WALK_PARAM}=true on '{name}'");
                    }
                }
            }
        }
        else
        {
            // Player not in range, stop moving
            if (agent != null && agent.isActiveAndEnabled && !isCharging)
            {
                agent.isStopped = false;
                agent.SetDestination(transform.position);
                agent.velocity = Vector3.zero;
            }
            if (animator != null && hasWalkParam)
                animator.SetBool(WALK_PARAM, false);
        }
    }
    
    void Attack()
    {
        isAttacking = true;
        lastAttackTime = Time.time;
        
        Debug.Log($"TankEnemy: Performing heavy attack on '{name}'");
        
        // Trigger attack animation
        if (animator != null && hasAttackParam)
            animator.SetTrigger(ATTACK_PARAM);
        
        // Deal damage to player
        if (player != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            if (distanceToPlayer <= attackRange)
            {
                var playerHealth = player.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(attackDamage);
                    Debug.Log($"Tank enemy attacked player for {attackDamage} damage!");
                }
                else
                {
                    Debug.LogWarning("Player hit by tank! (No PlayerHealth component found on player)");
                }
            }
        }
        
        // Reset attack state
        Invoke(nameof(ResetAttack), 1.5f); // Longer recovery for heavy attack
    }
    
    System.Collections.IEnumerator PerformChargeAttack()
    {
        if (player == null || isCharging || isDead) yield break;
        
        isCharging = true;
        lastChargeTime = Time.time;
        
        Debug.Log($"TankEnemy: Starting charge attack on '{name}'");
        
        // Trigger charge animation
        if (animator != null && hasChargeParam)
            animator.SetTrigger(CHARGE_PARAM);
        
        // Stop normal movement
        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
        
        // Calculate charge direction
        Vector3 chargeDirection = (player.position - transform.position).normalized;
        chargeDirection.y = 0;
        
        // Face charge direction
        if (chargeDirection != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(chargeDirection);
        
        // Brief wind-up period
        yield return new WaitForSeconds(0.5f);
        
        // Perform the charge
        Vector3 startPosition = transform.position;
        Vector3 targetPosition = startPosition + (chargeDirection * chargeDistance);
        
        // Ensure target position is valid
        if (NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, 2f, NavMesh.AllAreas))
        {
            targetPosition = hit.position;
        }
        
        float chargeDuration = chargeDistance / chargeSpeed;
        float elapsed = 0f;
        
        while (elapsed < chargeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / chargeDuration;
            
            Vector3 newPosition = Vector3.Lerp(startPosition, targetPosition, t);
            transform.position = newPosition;
            
            // Check for collision with player during charge
            if (player != null)
            {
                float distanceToPlayer = Vector3.Distance(transform.position, player.position);
                if (distanceToPlayer <= 2f) // Charge hit radius
                {
                    var playerHealth = player.GetComponent<PlayerHealth>();
                    if (playerHealth != null)
                    {
                        playerHealth.TakeDamage(chargeDamage);
                        Debug.Log($"Tank enemy charge hit player for {chargeDamage} damage!");
                    }
                    break; // Stop charging after hitting player
                }
            }
            
            yield return null;
        }
        
        // Ensure final position
        transform.position = targetPosition;
        
        // Recovery period
        isRecovering = true;
        Debug.Log($"TankEnemy: Charge complete, recovering for {chargeRecoveryTime} seconds");
        
        yield return new WaitForSeconds(chargeRecoveryTime);
        
        // Resume normal behavior
        isCharging = false;
        isRecovering = false;
        
        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.isStopped = false;
        }
        
        Debug.Log($"TankEnemy: Charge recovery complete on '{name}'");
    }
    
    void ResetAttack()
    {
        isAttacking = false;
    }
    
    public void TakeDamage(float damage)
    {
        if (isDead) return;
        
        health -= damage;
        Debug.Log($"Tank Enemy took {damage} damage. Health: {health:F1}/{maxHealth:F1}");
        
        if (health <= 0)
        {
            Debug.Log($"Tank Enemy died after taking {damage} damage");
            Die();
        }
    }
    
    public void Die()
    {
        isDead = true;
        isCharging = false;
        isRecovering = false;
        
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
            animator.SetTrigger(DEATH_PARAM);
        
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

        Debug.Log("Tank Enemy died!");
    }
    
    private void OnDestroy()
    {
        if (Application.isPlaying)
        {
            EnemySpawnManager spawnManager = FindFirstObjectByType<EnemySpawnManager>();
            if (spawnManager != null)
            {
                spawnManager.RegisterDeath();
            }
        }
    }
    
    // Coroutine to hide visuals after death animation
    System.Collections.IEnumerator HideAfterDeathAnimation()
    {
        yield return new WaitForSeconds(3f); // Longer for tank death
        
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
    
    System.Collections.IEnumerator SnapToGroundDelayed()
    {
        yield return new WaitForSeconds(1f);
        StartCoroutine(SmoothSnapToGround());
    }
    
    System.Collections.IEnumerator SmoothSnapToGround()
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
    
    System.Collections.IEnumerator DelayedAnimatorInitialization()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        
        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
            animator.Play("Armature|Idle", 0, 0f);
            
            Debug.Log($"TankEnemy: Delayed init - Testing animator parameters on '{name}'");
            if (hasWalkParam)
            {
                animator.SetBool(WALK_PARAM, false);
                Debug.Log("  - Set IsWalking to false");
            }
            
            Debug.Log($"TankEnemy: Delayed animator initialization completed for '{name}'");
        }
    }
    
    // Visual debugging
    void OnDrawGizmosSelected()
    {
        // Draw detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        // Draw attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // Draw charge range
        if (canCharge)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, chargeDistance);
        }
        
        // Draw line to player if in range
        if (player != null)
        {
            float distance = Vector3.Distance(transform.position, player.position);
            if (distance <= detectionRange)
            {
                Gizmos.color = distance <= attackRange ? Color.red : 
                              (distance <= chargeDistance ? Color.magenta : Color.yellow);
                Gizmos.DrawLine(transform.position, player.position);
            }
        }
    }
}

// Extension for animator parameter checking
static class TankEnemyAnimatorExtensions
{
    public static bool AnimatorHasParameter(this Animator animator, string paramName, AnimatorControllerParameterType type)
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
}
