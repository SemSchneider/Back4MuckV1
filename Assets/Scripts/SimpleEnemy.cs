using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class SimpleEnemy : MonoBehaviour
{
    [Header("Enemy Settings")]
    public float maxHealth = 100f;
    public float health = 100f;
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
    
    private float lastAttackTime;
    private bool isDead = false;
    private bool isAttacking = false;
    private bool hasWalkParam = false;
    private bool hasAttackParam = false;
    private bool hasDeathParam = false;
    
    [Header("Animator Runtime Settings")]
    public bool forceAnimatorAlwaysAnimate = true;
    public bool disableAnimatorRootMotion = true;
    
    // Animation parameter names
    private const string WALK_PARAM = "IsWalking";
    private const string ATTACK_PARAM = "Attack";
    private const string DEATH_PARAM = "Death";
    
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

        // Cache animator parameter availability and warn if missing
        if (animator == null)
        {
            Debug.LogWarning("SimpleEnemy: No Animator found on this GameObject or its children. Animations will not play.");
        }
        else
        {
            // Enforce safe runtime settings for spawned prefabs
            if (forceAnimatorAlwaysAnimate)
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            if (disableAnimatorRootMotion)
                animator.applyRootMotion = false;
            animator.enabled = true;
            animator.speed = 1f;
            if (animator.layerCount > 0)
                animator.SetLayerWeight(0, 1f);

            var controllerName = animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.name : "<None>";
            Debug.Log($"SimpleEnemy: Animator='{animator.name}', Controller='{controllerName}' on '{name}'");
            
            // Force reset animator state for spawned enemies
            if (animator.runtimeAnimatorController != null)
            {
                animator.Rebind();
                animator.Update(0f);
                Debug.Log($"SimpleEnemy: Forced animator rebind on '{name}'");
                
                // Additional setup for spawned enemies
                StartCoroutine(DelayedAnimatorInitialization());
            }

            // Log initial state for diagnostics
            if (animator.layerCount > 0)
            {
                var st = animator.GetCurrentAnimatorStateInfo(0);
                Debug.Log($"SimpleEnemy: Initial state normalizedTime={st.normalizedTime:F2} hash={st.shortNameHash} on '{name}'");
                
                // Test if animator can transition properly
                if (hasWalkParam)
                {
                    animator.SetBool(WALK_PARAM, true);
                    animator.Update(0f);
                    var walkState = animator.GetCurrentAnimatorStateInfo(0);
                    Debug.Log($"SimpleEnemy: After setting walk=true, state hash={walkState.shortNameHash} on '{name}'");
                    animator.SetBool(WALK_PARAM, false);
                }
            }

            hasWalkParam = animator.AnimatorHasParameter(WALK_PARAM, AnimatorControllerParameterType.Bool);
            hasAttackParam = animator.AnimatorHasParameter(ATTACK_PARAM, AnimatorControllerParameterType.Trigger);
            hasDeathParam = animator.AnimatorHasParameter(DEATH_PARAM, AnimatorControllerParameterType.Trigger);

            if (!hasWalkParam)
                Debug.LogWarning($"SimpleEnemy: Animator missing Bool parameter '{WALK_PARAM}'. Walking state won't switch.");
            if (!hasAttackParam)
                Debug.LogWarning($"SimpleEnemy: Animator missing Trigger parameter '{ATTACK_PARAM}'. Attack animation won't play.");
            if (!hasDeathParam)
                Debug.LogWarning($"SimpleEnemy: Animator missing Trigger parameter '{DEATH_PARAM}'. Death animation won't play.");
            if (animator.runtimeAnimatorController == null)
                Debug.LogWarning("SimpleEnemy: Animator has no Controller assigned. Assign your Zombie.controller (or equivalent).");
        }
            
        // Configure NavMesh Agent
        if (agent != null)
        {
            agent.speed = moveSpeed;
            agent.stoppingDistance = Mathf.Max(0f, attackRange - 0.5f); // Stop slightly before attack range
            agent.acceleration = 8f; // Faster acceleration
            agent.angularSpeed = 120f; // Faster turning
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        }
        
        // Configure Rigidbody to prevent falling
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true; // Prevents physics interference
            rb.useGravity = false; // NavMesh handles movement
        }

        // Calibrate agent vertical placement so the enemy sits on the NavMesh
        if (agent != null)
        {
            var capsule = GetComponent<CapsuleCollider>();
            if (capsule != null)
            {
                // Ensure agent size is at least collider size
                agent.height = Mathf.Max(agent.height, capsule.height);
                agent.radius = Mathf.Max(agent.radius, capsule.radius);

                // If pivot is at center (capsule.center.y ≈ 0), offset should be half height
                // General formula to bring collider bottom to NavMesh surface
                agent.baseOffset = Mathf.Max(0f, (capsule.height * 0.5f) - capsule.center.y);
            }

            // Snap onto NavMesh at start to avoid half-sinking from import pivots
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
        if (isDead) return;
        
        if (player == null) return;
        
        // Continuously ensure walking animation loops while moving
        if (animator != null && hasWalkParam && animator.GetBool(WALK_PARAM))
        {
            EnsureWalkingAnimationLoops();
        }
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        // Check if player is in detection range
        if (distanceToPlayer <= detectionRange)
        {
            // Face the player
            Vector3 lookDirection = (player.position - transform.position).normalized;
            lookDirection.y = 0; // Keep enemy upright
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
                    
                if (animator != null)
                {
                    if (hasWalkParam)
                        animator.SetBool(WALK_PARAM, false);
                }
                
                // Attack if cooldown is over
                if (Time.time - lastAttackTime >= attackCooldown && !isAttacking)
                {
                    Attack();
                }
            }
            else
            {
                // Move towards player
                if (agent != null && agent.isActiveAndEnabled)
                {
                    agent.isStopped = false;
                    agent.SetDestination(player.position);
                    if (animator != null)
                    {
                        if (hasWalkParam)
                        {
                            animator.SetBool(WALK_PARAM, true);
                            // Ensure walking animation loops properly
                            EnsureWalkingAnimationLoops();
                        }
                        
                        // Force animator update for spawned enemies
                        animator.Update(0f);
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
            if (animator != null)
            {
                if (hasWalkParam)
                    animator.SetBool(WALK_PARAM, false);
            }
        }
    }
    
    void Attack()
    {
        isAttacking = true;
        lastAttackTime = Time.time;
        
        // Trigger attack animation
        if (animator != null)
        {
            if (hasAttackParam)
                animator.SetTrigger(ATTACK_PARAM);
        }
        
        // Deal damage to player (check if player is still in range)
        if (player != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            if (distanceToPlayer <= attackRange)
            {
                // Try to get player health component
                var playerHealth = player.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(attackDamage);
                    Debug.Log($"Enemy attacked player for {attackDamage} damage!");
                }
                else
                {
                    Debug.LogWarning("Player hit! (No PlayerHealth component found on player)");
                }
            }
            else
            {
                Debug.Log("Enemy attack missed - player too far away");
            }
        }
        else
        {
            Debug.LogWarning("Enemy attack failed - no player reference");
        }
        
        // Reset attack state after animation
        Invoke(nameof(ResetAttack), 1f);
    }
    
    void ResetAttack()
    {
        isAttacking = false;
    }
    
    public void TakeDamage(float damage)
    {
        if (isDead) return;
        
        health -= damage;
        Debug.Log($"Enemy took {damage} damage. Health: {health:F1}/{100:F1}");
        
        if (health <= 0)
        {
            Debug.Log($"Enemy died after taking {damage} damage");
            Die();
        }
    }
    
    void Die()
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
        if (animator != null)
        {
            if (hasDeathParam)
                animator.SetTrigger(DEATH_PARAM);
        }
        
        // Snap zombie to ground level when dying (with small delay for animation)
        StartCoroutine(SnapToGroundDelayed());
        
        // Disable collider to prevent further interactions
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = false;

        // Hide visuals after death animation finishes
        if (hideVisualsOnDeath)
        {
            // Wait for death animation to play before hiding
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

        Debug.Log("Enemy died!");
    }
    
    // Coroutine to hide visuals after death animation
    System.Collections.IEnumerator HideAfterDeathAnimation()
    {
        // Wait for death animation to finish (adjust time as needed)
        yield return new WaitForSeconds(2f);
        
        var renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].enabled = false;
        }

        // Hide any world-space UI under this enemy (health bars, etc.)
        var canvases = GetComponentsInChildren<Canvas>(true);
        for (int i = 0; i < canvases.Length; i++)
        {
            canvases[i].enabled = false;
        }
    }
    
    // Coroutine to snap zombie to ground with delay
    System.Collections.IEnumerator SnapToGroundDelayed()
    {
        // Wait for death animation to play for 1 second
        yield return new WaitForSeconds(1f);
        StartCoroutine(SmoothSnapToGround());
    }
    
    // Smooth coroutine to gradually move zombie to ground
    System.Collections.IEnumerator SmoothSnapToGround()
    {
        Vector3 startPosition = transform.position;
        Vector3 targetPosition = startPosition;
        
        // Find ground level
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out hit, 10f))
        {
            targetPosition.y = hit.point.y;
            Debug.Log($"Smoothly moving zombie to ground at Y: {hit.point.y}");
        }
        else
        {
            // Fallback: try to find NavMesh ground
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit navHit, 5f, NavMesh.AllAreas))
            {
                targetPosition.y = navHit.position.y;
                Debug.Log($"Smoothly moving zombie to NavMesh ground at Y: {navHit.position.y}");
            }
            else
            {
                // No ground found, don't move
                yield break;
            }
        }
        
        // Smoothly move to ground over 0.5 seconds
        float duration = 0.5f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Use smooth step for more natural movement
            t = t * t * (3f - 2f * t);
            
            transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }
        
        // Ensure final position is exact
        transform.position = targetPosition;
        Debug.Log("Zombie smoothly moved to ground");
    }
    
    // Delayed animator initialization for spawned enemies
    System.Collections.IEnumerator DelayedAnimatorInitialization()
    {
        // Wait a few frames for full initialization
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        
        if (animator != null)
        {
            // Force another rebind and update
            animator.Rebind();
            animator.Update(0f);
            
            // Ensure proper initial state
            animator.Play("Armature|Idle", 0, 0f);
            
            // Test all parameters
            Debug.Log($"SimpleEnemy: Delayed init - Testing animator parameters on '{name}'");
            if (hasWalkParam)
            {
                animator.SetBool(WALK_PARAM, false);
                Debug.Log("  - Set IsWalking to false");
            }
            if (hasAttackParam)
            {
                Debug.Log("  - Attack parameter available");
            }
            if (hasDeathParam)
            {
                Debug.Log("  - Death parameter available");
            }
            
            Debug.Log($"SimpleEnemy: Delayed animator initialization completed for '{name}'");
        }
    }
    
    // Method to ensure walking animation loops properly
    void EnsureWalkingAnimationLoops()
    {
        if (animator != null && hasWalkParam)
        {
            // Get current walking state info
            var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            
            // Check if we're in the walking state and if it's near the end
            if (stateInfo.IsName("Armature|Walk"))
            {
                // If animation is near the end (90% complete), reset it to loop
                if (stateInfo.normalizedTime >= 0.9f)
                {
                    animator.Play("Armature|Walk", 0, 0f);
                    Debug.Log("Reset walking animation to loop");
                }
            }
        }
    }
    
    // Method to snap zombie to ground level (instant version)
    void SnapToGround()
    {
        // Cast a ray downward to find the ground
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out hit, 10f))
        {
            // Move the zombie to the ground level
            Vector3 newPosition = transform.position;
            newPosition.y = hit.point.y;
            transform.position = newPosition;
            
            Debug.Log($"Snapped zombie to ground at Y: {hit.point.y}");
        }
        else
        {
            // Fallback: try to find NavMesh ground
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit navHit, 5f, NavMesh.AllAreas))
            {
                Vector3 newPosition = transform.position;
                newPosition.y = navHit.position.y;
                transform.position = newPosition;
                
                Debug.Log($"Snapped zombie to NavMesh ground at Y: {navHit.position.y}");
            }
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

// Local helpers
static class SimpleEnemyAnimatorExtensions
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
