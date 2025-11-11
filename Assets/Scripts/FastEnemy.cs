using UnityEngine;
using UnityEngine.AI;
using System.Collections;

/// <summary>
/// Fast Enemy Archetype - Low health, fast movement, low damage, quick attacks
/// </summary>
public class FastEnemy : MonoBehaviour
{
    [Header("Fast Enemy Settings")]
    public float maxHealth = 50f;  // Lower health than SimpleEnemy
    public float health = 50f;
    public float moveSpeed = 6f;  // Much faster than SimpleEnemy
    public float detectionRange = 12f;  // Good detection range
    public float attackRange = 1.5f;  // Shorter attack range
    public float attackDamage = 15f;  // Lower damage
    public float attackCooldown = 0.8f;  // Faster attacks
    public bool stopPushingAtAttackRange = true;

    [Header("Fast Special Abilities")]
    public float dodgeSpeed = 8f;  // Speed during dodge
    public float dodgeDistance = 3f;  // Distance of dodge
    public float dodgeChance = 0.3f;  // 30% chance to dodge when taking damage
    public bool canDodge = true;  // Whether this fast enemy can dodge
    public float strafingSpeed = 4f;  // Speed when strafing around player
    public bool canStrafe = true;  // Whether to strafe around player

    public enum DeathBehavior { Instant, TimedDelay }
    [Header("Death Settings")]
    public DeathBehavior deathBehavior = DeathBehavior.TimedDelay;
    public float deathDestroyDelay = 0.3f;  // Shorter delay for fast enemy
    public bool hideVisualsOnDeath = true;
    
    [Header("Components")]
    public Transform player;
    public NavMeshAgent agent;
    public Animator animator;
    
    private float lastAttackTime;
    private float lastDodgeTime;
    private bool isDead = false;
    private bool isAttacking = false;
    private bool isDodging = false;
    private bool isStrafing = false;
    private Vector3 strafeDirection = Vector3.right;
    private float strafeTimer = 0f;
    private float strafeChangeInterval = 2f;
    private bool hasWalkParam = false;
    private bool hasAttackParam = false;
    private bool hasDeathParam = false;
    private bool hasDodgeParam = false;
    
    [Header("Animator Runtime Settings")]
    public bool forceAnimatorAlwaysAnimate = true;
    public bool disableAnimatorRootMotion = true;
    
    // Animation parameter names
    private const string WALK_PARAM = "IsWalking";
    private const string ATTACK_PARAM = "Attack";
    private const string DEATH_PARAM = "Death";
    private const string DODGE_PARAM = "Dodge";
    
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
            Debug.LogWarning($"FastEnemy: No Animator found on '{name}' or its children. Animations will not play.");
        }
        else
        {
            // Ensure Animator is enabled
            if (!animator.enabled)
            {
                animator.enabled = true;
                Debug.LogWarning($"FastEnemy: Animator was disabled on '{name}', enabling it.");
            }
            // Always animate (important for runtime clones)
            if (forceAnimatorAlwaysAnimate)
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            // Disable root motion if needed
            if (disableAnimatorRootMotion)
                animator.applyRootMotion = false;
            animator.speed = 1.2f; // Faster animation speed for fast enemy
            if (animator.layerCount > 0)
                animator.SetLayerWeight(0, 1f);

            // Check Animator Controller assignment
            if (animator.runtimeAnimatorController == null)
            {
                Debug.LogWarning($"FastEnemy: Animator on '{name}' has no Controller assigned.");
            }
            else
            {
                var controllerName = animator.runtimeAnimatorController.name;
                Debug.Log($"FastEnemy: Animator='{animator.name}', Controller='{controllerName}' on '{name}'");
                // Force rebind for runtime clones
                animator.Rebind();
                animator.Update(0f);
                Debug.Log($"FastEnemy: Forced animator rebind on '{name}'");
                // Additional setup for spawned enemies
                StartCoroutine(DelayedAnimatorInitialization());
            }

            // Cache animator parameter availability
            hasWalkParam = FastEnemyAnimatorExtensions.AnimatorHasParameter(animator, WALK_PARAM, AnimatorControllerParameterType.Bool);
            hasAttackParam = FastEnemyAnimatorExtensions.AnimatorHasParameter(animator, ATTACK_PARAM, AnimatorControllerParameterType.Trigger);
            hasDeathParam = FastEnemyAnimatorExtensions.AnimatorHasParameter(animator, DEATH_PARAM, AnimatorControllerParameterType.Trigger);
            hasDodgeParam = FastEnemyAnimatorExtensions.AnimatorHasParameter(animator, DODGE_PARAM, AnimatorControllerParameterType.Trigger);

            if (!hasWalkParam)
                Debug.LogWarning($"FastEnemy: Animator missing Bool parameter '{WALK_PARAM}' on '{name}'.");
            if (!hasAttackParam)
                Debug.LogWarning($"FastEnemy: Animator missing Trigger parameter '{ATTACK_PARAM}' on '{name}'.");
            if (!hasDeathParam)
                Debug.LogWarning($"FastEnemy: Animator missing Trigger parameter '{DEATH_PARAM}' on '{name}'.");
            if (!hasDodgeParam && canDodge)
                Debug.LogWarning($"FastEnemy: Animator missing Trigger parameter '{DODGE_PARAM}' on '{name}'. Dodge disabled.");
        }
            
        // Configure NavMesh Agent for fast behavior
        if (agent != null)
        {
            agent.speed = moveSpeed;
            agent.stoppingDistance = Mathf.Max(0f, attackRange - 0.2f);
            agent.acceleration = 12f; // Very fast acceleration
            agent.angularSpeed = 240f; // Very fast turning
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance; // Faster processing
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

        // Initialize strafe direction randomly
        strafeDirection = (Random.value > 0.5f) ? Vector3.right : Vector3.left;
    }
    
    void Update()
    {
        if (isDead || isDodging) return;
        
        if (player == null) return;
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        // Update strafe timer
        strafeTimer += Time.deltaTime;
        if (strafeTimer >= strafeChangeInterval)
        {
            strafeDirection = -strafeDirection; // Switch strafe direction
            strafeTimer = 0f;
        }
        
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
            
            // Check if in attack range
            if (distanceToPlayer <= attackRange)
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
                
                isStrafing = false;
                
                // Attack if cooldown is over
                if (Time.time - lastAttackTime >= attackCooldown && !isAttacking)
                {
                    Attack();
                }
            }
            else
            {
                // Move towards player with strafing behavior
                if (agent != null && agent.isActiveAndEnabled)
                {
                    Vector3 targetPosition;
                    
                    // Use strafing if enabled and close enough
                    if (canStrafe && distanceToPlayer <= 6f && distanceToPlayer > attackRange)
                    {
                        isStrafing = true;
                        // Calculate strafe position
                        Vector3 directionToPlayer = (player.position - transform.position).normalized;
                        Vector3 perpendicular = Vector3.Cross(directionToPlayer, Vector3.up);
                        targetPosition = player.position + (perpendicular * strafeDirection.x * 3f);
                        
                        // Ensure strafe position is valid
                        if (NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                        {
                            targetPosition = hit.position;
                        }
                        else
                        {
                            // Fallback to direct approach if strafe position invalid
                            targetPosition = player.position;
                            isStrafing = false;
                        }
                        
                        // Set strafe speed
                        agent.speed = strafingSpeed;
                    }
                    else
                    {
                        // Direct approach
                        isStrafing = false;
                        targetPosition = player.position;
                        agent.speed = moveSpeed;
                    }
                    
                    agent.isStopped = false;
                    agent.SetDestination(targetPosition);
                    
                    if (animator != null && hasWalkParam)
                    {
                        animator.SetBool(WALK_PARAM, true);
                        Debug.Log($"FastEnemy: Set {WALK_PARAM}=true on '{name}' (strafing: {isStrafing})");
                    }
                }
            }
        }
        else
        {
            // Player not in range, stop moving
            if (agent != null && agent.isActiveAndEnabled)
            {
                agent.isStopped = false;
                agent.SetDestination(transform.position);
                agent.velocity = Vector3.zero;
            }
            if (animator != null && hasWalkParam)
                animator.SetBool(WALK_PARAM, false);
            
            isStrafing = false;
        }
    }
    
    void Attack()
    {
        isAttacking = true;
        lastAttackTime = Time.time;
        
        Debug.Log($"FastEnemy: Performing quick attack on '{name}'");
        
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
                    Debug.Log($"Fast enemy attacked player for {attackDamage} damage!");
                }
                else
                {
                    Debug.LogWarning("Player hit by fast enemy! (No PlayerHealth component found on player)");
                }
            }
        }
        
        // Reset attack state
        Invoke(nameof(ResetAttack), 0.5f); // Quick recovery for fast attack
    }
    
    void ResetAttack()
    {
        isAttacking = false;
    }
    
    public void TakeDamage(float damage)
    {
        if (isDead) return;
        
        // Try to dodge if able and chance succeeds
        if (canDodge && !isDodging && Random.value <= dodgeChance && Time.time - lastDodgeTime >= 2f)
        {
            StartCoroutine(PerformDodge());
            // Still take damage but reduced
            damage *= 0.5f;
            Debug.Log($"FastEnemy: Dodged! Damage reduced to {damage}");
        }
        
        health -= damage;
        Debug.Log($"Fast Enemy took {damage} damage. Health: {health:F1}/{maxHealth:F1}");
        
        if (health <= 0)
        {
            Debug.Log($"Fast Enemy died after taking {damage} damage");
            Die();
        }
    }
    
    System.Collections.IEnumerator PerformDodge()
    {
        if (isDodging || isDead) yield break;
        
        isDodging = true;
        lastDodgeTime = Time.time;
        
        Debug.Log($"FastEnemy: Performing dodge on '{name}'");
        
        // Trigger dodge animation
        if (animator != null && hasDodgeParam)
            animator.SetTrigger(DODGE_PARAM);
        
        // Stop normal movement
        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
        
        // Calculate dodge direction (perpendicular to player direction)
        Vector3 dodgeDirection = Vector3.right;
        if (player != null)
        {
            Vector3 directionToPlayer = (player.position - transform.position).normalized;
            Vector3 perpendicular = Vector3.Cross(directionToPlayer, Vector3.up);
            dodgeDirection = (Random.value > 0.5f) ? perpendicular : -perpendicular;
        }
        
        // Perform the dodge
        Vector3 startPosition = transform.position;
        Vector3 targetPosition = startPosition + (dodgeDirection * dodgeDistance);
        
        // Ensure target position is valid
        if (NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, 2f, NavMesh.AllAreas))
        {
            targetPosition = hit.position;
        }
        else
        {
            // Can't dodge to that position, just stay in place
            targetPosition = startPosition;
        }
        
        float dodgeDuration = 0.3f;
        float elapsed = 0f;
        
        while (elapsed < dodgeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dodgeDuration;
            
            Vector3 newPosition = Vector3.Lerp(startPosition, targetPosition, t);
            transform.position = newPosition;
            
            yield return null;
        }
        
        // Ensure final position
        transform.position = targetPosition;
        
        // Resume normal behavior
        isDodging = false;
        
        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.isStopped = false;
        }
        
        Debug.Log($"FastEnemy: Dodge complete on '{name}'");
    }
    
    public void Die()
    {
        isDead = true;
        isDodging = false;
        isStrafing = false;
        
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

        Debug.Log("Fast Enemy died!");
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
        yield return new WaitForSeconds(1f); // Shorter for fast enemy
        
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
        yield return new WaitForSeconds(0.5f);
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
        
        float duration = 0.3f; // Faster for fast enemy
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
            
            Debug.Log($"FastEnemy: Delayed init - Testing animator parameters on '{name}'");
            if (hasWalkParam)
            {
                animator.SetBool(WALK_PARAM, false);
                Debug.Log("  - Set IsWalking to false");
            }
            
            Debug.Log($"FastEnemy: Delayed animator initialization completed for '{name}'");
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
        
        // Draw dodge visualization
        if (canDodge)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, dodgeDistance);
        }
        
        // Draw strafe area
        if (canStrafe && player != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            if (distanceToPlayer <= 6f)
            {
                Gizmos.color = Color.green;
                Vector3 directionToPlayer = (player.position - transform.position).normalized;
                Vector3 perpendicular = Vector3.Cross(directionToPlayer, Vector3.up);
                
                Vector3 strafeLeft = player.position + (perpendicular * 3f);
                Vector3 strafeRight = player.position - (perpendicular * 3f);
                
                Gizmos.DrawLine(strafeLeft, strafeRight);
            }
        }
        
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

// Extension for animator parameter checking
static class FastEnemyAnimatorExtensions
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
