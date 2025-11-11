using UnityEngine;
using UnityEngine.AI;
using System.Collections;

/// <summary>
/// Simple Enemy - Balanced baseline enemy with standard stats
/// </summary>
public class SimpleEnemy : MonoBehaviour
{
    #region Enemy Configuration

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
    
    [Header("Feedback Effects")]
    public GameObject bloodHitEffectPrefab;
    public Transform hitEffectSpawnPoint; // Optional: specific point to spawn effects, defaults to center of enemy
    public bool enableHitFeedback = true;
    public float hitFeedbackDuration = 0.3f;
    public Color hitFlashColor = Color.red;
    
    [Header("Animator Runtime Settings")]
    public bool forceAnimatorAlwaysAnimate = true;
    public bool disableAnimatorRootMotion = true;

    #endregion

    #region Private Fields

    private float lastAttackTime;
    private bool isDead = false;
    private bool isAttacking = false;
    private bool hasWalkParam = false;
    private bool hasAttackParam = false;
    private bool hasDeathParam = false;
    
    private Renderer[] enemyRenderers;
    private Material[] originalMaterials;
    private Material[] flashMaterials;

    #endregion

    #region Animation Constants
    
    private const string WALK_PARAM = "IsWalking";
    private const string ATTACK_PARAM = "Attack";
    private const string DEATH_PARAM = "Death";

    #endregion

    #region Unity Lifecycle
    
    void Start()
    {
        InitializeHealth();
        FindPlayer();
        GetComponents();
        SetupAnimator();
        SetupHitFeedback();
        ConfigureNavMeshAgent();
        ConfigurePhysics();
        CalibrateVerticalPlacement();
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
            FacePlayer();
            
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

    #endregion

    #region Initialization Methods

    private void InitializeHealth()
    {
        // Initialize health from maxHealth at start
        health = Mathf.Clamp(maxHealth, 1f, Mathf.Infinity);
    }

    private void FindPlayer()
    {
        // Find player if not assigned
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }
    }

    private void GetComponents()
    {
        // Get components if not assigned
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();
        if (animator == null)
            animator = GetComponent<Animator>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    private void SetupAnimator()
    {
        // Runtime animator checks for clones
        if (animator == null)
        {
            Debug.LogWarning($"SimpleEnemy: No Animator found on '{name}' or its children. Animations will not play.");
            return;
        }

        // Ensure Animator is enabled
        if (!animator.enabled)
        {
            animator.enabled = true;
            Debug.LogWarning($"SimpleEnemy: Animator was disabled on '{name}', enabling it.");
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

        CheckAnimatorController();
        CacheAnimatorParameters();
    }

    private void CheckAnimatorController()
    {
        if (animator.runtimeAnimatorController == null)
        {
            Debug.LogWarning($"SimpleEnemy: Animator on '{name}' has no Controller assigned. Assign your Zombie.controller (or equivalent) in the prefab.");
            return;
        }

        var controllerName = animator.runtimeAnimatorController.name;
        Debug.Log($"SimpleEnemy: Animator='{animator.name}', Controller='{controllerName}' on '{name}'");
        
        // Force rebind for runtime clones
        animator.Rebind();
        animator.Update(0f);
        Debug.Log($"SimpleEnemy: Forced animator rebind on '{name}'");
        
        // Additional setup for spawned enemies
        StartCoroutine(DelayedAnimatorInitialization());
    }

    private void CacheAnimatorParameters()
    {
        // Cache animator parameter availability
        hasWalkParam = SimpleEnemyAnimatorExtensions.AnimatorHasParameter(animator, WALK_PARAM, AnimatorControllerParameterType.Bool);
        hasAttackParam = SimpleEnemyAnimatorExtensions.AnimatorHasParameter(animator, ATTACK_PARAM, AnimatorControllerParameterType.Trigger);
        hasDeathParam = SimpleEnemyAnimatorExtensions.AnimatorHasParameter(animator, DEATH_PARAM, AnimatorControllerParameterType.Trigger);

        if (!hasWalkParam)
            Debug.LogWarning($"SimpleEnemy: Animator missing Bool parameter '{WALK_PARAM}' on '{name}'. Walking state won't switch.");
        if (!hasAttackParam)
            Debug.LogWarning($"SimpleEnemy: Animator missing Trigger parameter '{ATTACK_PARAM}' on '{name}'. Attack animation won't play.");
        if (!hasDeathParam)
            Debug.LogWarning($"SimpleEnemy: Animator missing Trigger parameter '{DEATH_PARAM}' on '{name}'. Death animation won't play.");
    }

    private void SetupHitFeedback()
    {
        // Initialize hit feedback materials and renderers
        if (enableHitFeedback)
        {
            InitializeHitFeedback();
        }
    }

    private void ConfigureNavMeshAgent()
    {
        if (agent == null) return;
        
        agent.speed = moveSpeed;
        agent.stoppingDistance = Mathf.Max(0f, attackRange - 0.5f); // Stop slightly before attack range
        agent.acceleration = 8f; // Faster acceleration
        agent.angularSpeed = 120f; // Faster turning
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
    }

    private void ConfigurePhysics()
    {
        // Configure Rigidbody to prevent falling
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true; // Prevents physics interference
            rb.useGravity = false; // NavMesh handles movement
        }
    }

    private void CalibrateVerticalPlacement()
    {
        // Calibrate agent vertical placement so the enemy sits on the NavMesh
        if (agent == null) return;

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

    #endregion

    #region Behavior Methods

    private void FacePlayer()
    {
        if (player == null) return;
        
        Vector3 lookDirection = (player.position - transform.position).normalized;
        lookDirection.y = 0; // Keep enemy upright
        if (lookDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(lookDirection);
        }
    }

    private void HandleAttackRange()
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
        {
            animator.SetBool(WALK_PARAM, false);
        }
        
        // Attack if cooldown is over
        if (Time.time - lastAttackTime >= attackCooldown && !isAttacking)
        {
            Attack();
        }
    }

    private void HandleMovementToPlayer()
    {
        // Move towards player
        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
            if (animator != null && hasWalkParam)
            {
                animator.SetBool(WALK_PARAM, true);
                Debug.Log($"SimpleEnemy: Set {WALK_PARAM}=true on '{name}'");
                
                // Check if transition actually happened
                StartCoroutine(CheckAnimationTransition());
                
                // Ensure walking animation loops properly
                EnsureWalkingAnimationLoops();
            }
            
            // Force animator update for spawned enemies
            if (animator != null)
                animator.Update(0f);
        }
    }

    private void HandleOutOfRange()
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
    }

    #endregion

    #region Combat Methods
    
    void Attack()
    {
        isAttacking = true;
        lastAttackTime = Time.time;
        
        // Trigger attack animation
        if (animator != null && hasAttackParam)
            animator.SetTrigger(ATTACK_PARAM);
        
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
        Debug.Log($"Enemy took {damage} damage. Health: {health:F1}/{maxHealth:F1}");
        
        // Trigger hit feedback
        if (enableHitFeedback)
        {
            TriggerHitFeedback();
        }
        
        if (health <= 0)
        {
            Debug.Log($"Enemy died after taking {damage} damage");
            Die();
        }
    }

    #endregion

    #region Hit Feedback Methods
    
    private void InitializeHitFeedback()
    {
        // Get all renderers on this enemy
        enemyRenderers = GetComponentsInChildren<Renderer>();
        if (enemyRenderers.Length == 0)
        {
            Debug.LogWarning($"SimpleEnemy: No renderers found for hit feedback on '{name}'");
            return;
        }
        
        // Store original materials
        originalMaterials = new Material[enemyRenderers.Length];
        flashMaterials = new Material[enemyRenderers.Length];
        
        for (int i = 0; i < enemyRenderers.Length; i++)
        {
            originalMaterials[i] = enemyRenderers[i].material;
            
            // Create a copy of the material and modify its color for flash effect
            flashMaterials[i] = new Material(originalMaterials[i]);
            flashMaterials[i].color = Color.Lerp(originalMaterials[i].color, hitFlashColor, 0.7f);
        }
    }
    
    private void TriggerHitFeedback()
    {
        // Visual feedback - spawn blood effect
        SpawnBloodEffect();
        
        // Audio feedback
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayZombieHitSound(transform.position);
        }
        
        // Color flash feedback
        if (enemyRenderers != null && enemyRenderers.Length > 0)
        {
            StartCoroutine(FlashHitColor());
        }
    }
    
    private void SpawnBloodEffect()
    {
        if (bloodHitEffectPrefab != null)
        {
            Vector3 effectPosition;
            
            // Use specified hit point or default to center
            if (hitEffectSpawnPoint != null)
            {
                effectPosition = hitEffectSpawnPoint.position;
            }
            else
            {
                effectPosition = transform.position + Vector3.up * (GetComponent<Collider>()?.bounds.size.y * 0.5f ?? 1f);
            }
            
            GameObject bloodEffect = Instantiate(bloodHitEffectPrefab, effectPosition, Quaternion.identity);
            
            // Destroy the effect after a short time
            Destroy(bloodEffect, 5f);
        }
    }
    
    private IEnumerator FlashHitColor()
    {
        if (enemyRenderers == null || flashMaterials == null) yield break;
        
        // Apply flash materials
        for (int i = 0; i < enemyRenderers.Length; i++)
        {
            if (enemyRenderers[i] != null && flashMaterials[i] != null)
            {
                enemyRenderers[i].material = flashMaterials[i];
            }
        }
        
        // Wait for the feedback duration
        yield return new WaitForSeconds(hitFeedbackDuration);
        
        // Restore original materials
        for (int i = 0; i < enemyRenderers.Length; i++)
        {
            if (enemyRenderers[i] != null && originalMaterials[i] != null)
            {
                enemyRenderers[i].material = originalMaterials[i];
            }
        }
    }

    #endregion

    #region Death Methods
    
    public void Die()
    {
        isDead = true;
        
        // Play death sound
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayZombieDeathSound(transform.position);
        }
        
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
    
    private void OnDestroy()
    {
        // Only register death if the application is playing (not during scene unload)
        if (Application.isPlaying)
        {
            // Find and notify EnemySpawnManager of this enemy's death
            EnemySpawnManager spawnManager = FindFirstObjectByType<EnemySpawnManager>();
            if (spawnManager != null)
            {
                spawnManager.RegisterDeath();
            }
            else
            {
                Debug.LogWarning("SimpleEnemy: EnemySpawnManager not found when enemy died");
            }
        }
    }

    #endregion

    #region Coroutines
    
    // Coroutine to hide visuals after death animation
    private IEnumerator HideAfterDeathAnimation()
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
    private IEnumerator SnapToGroundDelayed()
    {
        // Wait for death animation to play for 1 second
        yield return new WaitForSeconds(1f);
        StartCoroutine(SmoothSnapToGround());
    }
    
    // Smooth coroutine to gradually move zombie to ground
    private IEnumerator SmoothSnapToGround()
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
    private IEnumerator DelayedAnimatorInitialization()
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
    
    // Check if animation transition actually happened
    private IEnumerator CheckAnimationTransition()
    {
        yield return new WaitForSeconds(0.1f);
        
        if (animator != null)
        {
            var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            string currentState = stateInfo.IsName("Armature|Walk") ? "Walking" : 
                                 stateInfo.IsName("Armature|Idle") ? "Idle" : 
                                 stateInfo.IsName("Armature|Attack") ? "Attack" : 
                                 stateInfo.IsName("Armature|Die") ? "Death" : "Unknown";
            
            Debug.Log($"SimpleEnemy: Animation check - Current state: {currentState} (normalizedTime: {stateInfo.normalizedTime:F2}) on '{name}'");
            
            // If still in idle when should be walking, force the transition
            if (stateInfo.IsName("Armature|Idle") && hasWalkParam && animator.GetBool(WALK_PARAM))
            {
                Debug.LogWarning($"SimpleEnemy: Forcing walk transition on '{name}' - stuck in idle!");
                animator.Play("Armature|Walk", 0, 0f);
            }
        }
    }

    #endregion

    #region Animation Helper Methods
    
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

    #endregion

    #region Debug Methods
    
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

    #endregion
}

/// <summary>
/// Extension methods for Animator parameter checking
/// </summary>
public static class SimpleEnemyAnimatorExtensions
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