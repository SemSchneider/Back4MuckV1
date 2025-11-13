using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System;

/// <summary>
/// Fast Enemy Archetype - Low health, fast movement, low damage, quick attacks, dodge ability
/// </summary>
public class FastEnemy : MonoBehaviour
{
    // Uncomment the line below to enable verbose fast enemy debugging
    // #define DEBUG_FAST_ENEMY
    
    #region Events
    
    // Static event for when any fast enemy dies
    public static event Action<GameObject> OnEnemyDied;
    
    #endregion

    [Header("Fast Enemy Settings")]
    public float maxHealth = 50f;  // Lower health than SimpleEnemy
    public float health = 50f;
    public float moveSpeed = 8f;  // Much faster than SimpleEnemy (increased from 6f)
    public float detectionRange = 12f;  // Good detection range
    public float attackRange = 2.2f;  // Slightly longer attack range (increased from 1.5f)
    public float attackDamage = 15f;  // Lower damage
    public float attackCooldown = 0.6f;  // Faster attacks (improved from 0.8f)
    public bool stopPushingAtAttackRange = true;

    [Header("Fast Special Abilities")]
    public float dodgeSpeed = 10f;  // Speed during dodge (increased)
    public float dodgeDistance = 4f;  // Distance of dodge (increased)
    public float dodgeChance = 0.4f;  // 40% chance to dodge when taking damage (increased)
    public bool canDodge = true;  // Whether this fast enemy can dodge
    public float strafingSpeed = 6f;  // Speed when strafing around player (increased)
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
    
    [Header("Feedback Effects")]
    public GameObject bloodHitEffectPrefab;
    public Transform hitEffectSpawnPoint; // Optional: specific point to spawn effects, defaults to center of enemy
    public bool enableHitFeedback = true;
    public float hitFeedbackDuration = 0.2f; // Shorter for fast enemy
    public Color hitFlashColor = Color.red;
    
    private bool hasHitParam = false;
    private Renderer[] enemyRenderers;
    private Material[] originalMaterials;
    private Material[] flashMaterials;
    
    [Header("Animator Runtime Settings")]
    public bool forceAnimatorAlwaysAnimate = true;
    public bool disableAnimatorRootMotion = true;
    
    // Animation parameter names
    private const string WALK_PARAM = "IsWalking";
    private const string ATTACK_PARAM = "Attack";
    private const string DEATH_PARAM = "Death";
    private const string DODGE_PARAM = "Dodge";
    private const string HIT_PARAM = "Hit";
    
    void Start()
    {
        Debug.Log($"FastEnemy: Starting initialization for '{name}'");
        
        // Initialize health from maxHealth at start
        health = Mathf.Clamp(maxHealth, 1f, Mathf.Infinity);
        
        // Find player if not assigned
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                Debug.Log($"FastEnemy: Found player '{playerObj.name}'");
            }
            else
            {
                Debug.LogWarning($"FastEnemy: No GameObject with 'Player' tag found! Make sure your player has the 'Player' tag.");
            }
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
                
                // Check if the model has an avatar
                if (animator.avatar == null)
                {
                    Debug.LogWarning($"FastEnemy: No Avatar assigned to animator on '{name}'. This may cause animation issues. Consider setting Animation Type to 'Generic' in the model import settings.");
                    
                    // For models without avatars, ensure Generic animation type
                    if (animator.isHuman)
                    {
                        Debug.LogError($"FastEnemy: Animator is set to Humanoid but no Avatar is assigned on '{name}'. Change the model's Animation Type to 'Generic' in import settings.");
                    }
                    else
                    {
                        Debug.Log($"FastEnemy: Using Generic animation type (recommended for models without avatars) on '{name}'");
                    }
                }
                else
                {
                    Debug.Log($"FastEnemy: Avatar '{animator.avatar.name}' assigned, animation type: {(animator.isHuman ? "Humanoid" : "Generic")} on '{name}'");
                }
                
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
            hasHitParam = FastEnemyAnimatorExtensions.AnimatorHasParameter(animator, HIT_PARAM, AnimatorControllerParameterType.Trigger);

            // Debug all available parameters
            Debug.Log($"FastEnemy: Checking animator controller '{animator.runtimeAnimatorController.name}' on '{name}'");
            var allParams = animator.parameters;
            Debug.Log($"FastEnemy: Found {allParams.Length} parameters:");
            for (int i = 0; i < allParams.Length; i++)
            {
                var param = allParams[i];
                Debug.Log($"  - {param.name} ({param.type})");
            }

            // Log parameter status for debugging
            Debug.Log($"FastEnemy: Parameter mapping - Walk:{hasWalkParam}, Attack:{hasAttackParam}, Death:{hasDeathParam}, Dodge:{hasDodgeParam}, Hit:{hasHitParam}");
        }
        
        // Initialize hit feedback materials and renderers
        if (enableHitFeedback)
        {
            InitializeHitFeedback();
        }
            
        // Configure NavMesh Agent for fast behavior
        if (agent != null)
        {
            agent.speed = moveSpeed;
            agent.stoppingDistance = Mathf.Max(0.1f, attackRange - 0.5f); // Give more room to get close
            agent.acceleration = 12f; // Very fast acceleration
            agent.angularSpeed = 240f; // Very fast turning
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance; // Faster processing
            
            Debug.Log($"FastEnemy: Agent configured - Speed: {agent.speed}, StoppingDistance: {agent.stoppingDistance}, AttackRange: {attackRange}");
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
        strafeDirection = (UnityEngine.Random.value > 0.5f) ? Vector3.right : Vector3.left;
    }
    
    void Update()
    {
        if (isDead || isDodging) return;
        
        if (player == null) 
        {
            if (Time.frameCount % 60 == 0) // Log once per second at 60fps
                Debug.LogWarning($"FastEnemy: Player is null on '{name}'!");
            return;
        }
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        // Continuously ensure walking animation loops while moving (like SimpleEnemy)
        if (animator != null && hasWalkParam && animator.GetBool(WALK_PARAM))
        {
            EnsureWalkingAnimationLoops();
        }
        
        // Debug log occasionally
#if DEBUG_FAST_ENEMY
        if (Time.frameCount % 60 == 0) // Once per second at 60fps
        {
            Debug.Log($"FastEnemy '{name}': Distance to player: {distanceToPlayer:F2}, Detection range: {detectionRange:F2}, In range: {distanceToPlayer <= detectionRange}");
        }
#endif
        
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
                Debug.Log($"FastEnemy: In attack range! Distance: {distanceToPlayer:F2}, Attack range: {attackRange:F2}");
                
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
#if DEBUG_FAST_ENEMY
                    Debug.Log($"FastEnemy: Set {WALK_PARAM}=false (in attack range) on '{name}'");
#endif
                }
                
                isStrafing = false;
                
                // Attack if cooldown is over
                if (Time.time - lastAttackTime >= attackCooldown && !isAttacking)
                {
                    Debug.Log($"FastEnemy: Cooldown ready, attacking! Time since last attack: {Time.time - lastAttackTime:F2}");
                    Attack();
                }
                else
                {
                    Debug.Log($"FastEnemy: Attack on cooldown. Time since last attack: {Time.time - lastAttackTime:F2}, cooldown: {attackCooldown:F2}");
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
                    
                    // Simple walking animation like SimpleEnemy
                    if (animator != null && hasWalkParam)
                    {
                        animator.SetBool(WALK_PARAM, true);
#if DEBUG_FAST_ENEMY
                        Debug.Log($"FastEnemy: Set {WALK_PARAM}=true on '{name}' (strafing: {isStrafing})");
#endif
                        
                        // Force animator update for spawned enemies
                        animator.Update(0f);
                        
                        // Ensure walking animation loops properly
                        EnsureWalkingAnimationLoops();
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
            {
                animator.SetBool(WALK_PARAM, false);
#if DEBUG_FAST_ENEMY
                Debug.Log($"FastEnemy: Set {WALK_PARAM}=false (out of range) on '{name}'");
#endif
            }
            
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
        {
            animator.SetTrigger(ATTACK_PARAM);
            Debug.Log($"FastEnemy: Triggered {ATTACK_PARAM} animation on '{name}'");
        }
        else if (animator != null)
        {
            Debug.LogWarning($"FastEnemy: Cannot trigger {ATTACK_PARAM} - parameter not found on '{name}'");
        }
        
        // Deal damage to player
        if (player != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            Debug.Log($"FastEnemy: Attack - Distance to player: {distanceToPlayer:F2}, Attack range: {attackRange:F2}");
            
            if (distanceToPlayer <= attackRange)
            {
                var playerHealth = player.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(attackDamage);
                    Debug.Log($"FastEnemy: Successfully attacked player for {attackDamage} damage!");
                }
                else
                {
                    Debug.LogWarning($"FastEnemy: Player found but no PlayerHealth component! Player name: '{player.name}', tag: '{player.tag}'");
                }
            }
            else
            {
                Debug.Log($"FastEnemy: Player out of attack range. Distance: {distanceToPlayer:F2}, needed: {attackRange:F2}");
            }
        }
        else
        {
            Debug.LogWarning("FastEnemy: Player reference is null during attack!");
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
        if (canDodge && !isDodging && UnityEngine.Random.value <= dodgeChance && Time.time - lastDodgeTime >= 2f)
        {
            StartCoroutine(PerformDodge());
            // Still take damage but reduced
            damage *= 0.5f;
            Debug.Log($"FastEnemy: Dodged! Damage reduced to {damage}");
        }
        
        health -= damage;
#if DEBUG_FAST_ENEMY
        Debug.Log($"Fast Enemy took {damage} damage. Health: {health:F1}/{maxHealth:F1}");
#endif
        
        // Trigger hit feedback
        if (enableHitFeedback)
        {
            TriggerHitFeedback();
        }
        
        if (health <= 0)
        {
#if DEBUG_FAST_ENEMY
            Debug.Log($"Fast Enemy died after taking {damage} damage");
#endif
            Die();
        }
    }
    
    private void InitializeHitFeedback()
    {
        // Get all renderers on this enemy
        enemyRenderers = GetComponentsInChildren<Renderer>();
        if (enemyRenderers.Length == 0)
        {
            Debug.LogWarning($"FastEnemy: No renderers found for hit feedback on '{name}'");
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
        
        // Animation feedback
        if (animator != null && hasHitParam)
        {
            animator.SetTrigger(HIT_PARAM);
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
            Destroy(bloodEffect, 3f); // Shorter for fast enemy
        }
    }
    
    private System.Collections.IEnumerator FlashHitColor()
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
        
        // Wait for the feedback duration (shorter for fast enemy)
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
    
    System.Collections.IEnumerator PerformDodge()
    {
        if (isDodging || isDead) yield break;
        
        isDodging = true;
        lastDodgeTime = Time.time;
        
        Debug.Log($"FastEnemy: Performing dodge on '{name}'");
        
        // Trigger dodge animation
        if (animator != null && hasDodgeParam)
        {
            animator.SetTrigger(DODGE_PARAM);
            Debug.Log($"FastEnemy: Triggered {DODGE_PARAM} animation on '{name}'");
        }
        else if (animator != null)
        {
            Debug.LogWarning($"FastEnemy: Cannot trigger {DODGE_PARAM} - parameter not found on '{name}'");
        }
        
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
            dodgeDirection = (UnityEngine.Random.value > 0.5f) ? perpendicular : -perpendicular;
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
        if (isDead) return; // Prevent multiple death calls
        
        isDead = true;
        isDodging = false;
        isStrafing = false;
        
        // Notify that this fast enemy has died
        OnEnemyDied?.Invoke(gameObject);
        
        Debug.Log($"FastEnemy: Starting death sequence for '{name}'");
        
        // Play death sound
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayZombieDeathSound(transform.position);
        }
        
        // Stop movement immediately
        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.velocity = Vector3.zero;
            agent.enabled = false;
        }
        
        // Stop walking animation and trigger death animation
        if (animator != null)
        {
            if (hasWalkParam)
            {
                animator.SetBool(WALK_PARAM, false);
                Debug.Log($"FastEnemy: Set {WALK_PARAM}=false for death on '{name}'");
            }
            
            if (hasDeathParam)
            {
                animator.SetTrigger(DEATH_PARAM);
                Debug.Log($"FastEnemy: Triggered {DEATH_PARAM} animation on '{name}'");
                
                // Force animator update to process the trigger
                animator.Update(0f);
                
                // Check if death animation started
                StartCoroutine(MonitorDeathAnimation());
            }
            else
            {
                Debug.LogWarning($"FastEnemy: Cannot trigger {DEATH_PARAM} - parameter not found on '{name}'");
            }
        }
        
        // Snap to ground
        StartCoroutine(SnapToGroundDelayed());
        
        // Disable collider
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = false;

        // Handle destruction based on death behavior
        if (deathBehavior == DeathBehavior.Instant)
        {
            Debug.Log($"FastEnemy: Instant death - destroying immediately on '{name}'");
            Destroy(gameObject);
        }
        else
        {
            Debug.Log($"FastEnemy: Timed death - will destroy in {deathDestroyDelay} seconds on '{name}'");
            
            // Hide visuals after death animation
            if (hideVisualsOnDeath)
            {
                StartCoroutine(HideAfterDeathAnimation());
            }
            
            // Destroy after delay
            StartCoroutine(DestroyAfterDelay());
        }

        Debug.Log($"FastEnemy: Death sequence initiated for '{name}'");
    }
    
    // Method to ensure walking animation loops properly (from SimpleEnemy)
    void EnsureWalkingAnimationLoops()
    {
        if (animator != null && hasWalkParam)
        {
            // Get current walking state info
            var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            
            // Check if we're in the walking state and if it's near the end
            if (stateInfo.IsName("Walk") || stateInfo.IsName("Armature|Walk"))
            {
                // If animation is near the end (90% complete), reset it to loop
                if (stateInfo.normalizedTime >= 0.9f)
                {
                    animator.Play("Walk", 0, 0f);
                    Debug.Log($"FastEnemy: Reset walking animation to loop on '{name}'");
                }
            }
        }
    }
    
    // Verify walking animation is properly looping
    System.Collections.IEnumerator VerifyWalkingAnimation()
    {
        yield return new WaitForSeconds(0.5f); // Wait for transition to complete
        
        if (animator != null && hasWalkParam && animator.GetBool(WALK_PARAM))
        {
            var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            
            // Check if we're in a walking state
            bool isInWalkState = stateInfo.IsName("Walk") || 
                                stateInfo.IsName("Walking") || 
                                stateInfo.IsName("Run") ||
                                stateInfo.IsName("Move") ||
                                stateInfo.normalizedTime > 0.1f; // Or if animation is progressing
            
            if (isInWalkState)
            {
                Debug.Log($"FastEnemy: Walking animation verified on '{name}' - State: {stateInfo.shortNameHash}, Time: {stateInfo.normalizedTime:F2}");
            }
            else
            {
                Debug.LogWarning($"FastEnemy: Walking animation may not be playing correctly on '{name}' - State: {stateInfo.shortNameHash}, Time: {stateInfo.normalizedTime:F2}");
                
                // Try to force the walking state again
                if (hasWalkParam && agent != null && agent.velocity.magnitude > 0.1f)
                {
                    animator.SetBool(WALK_PARAM, false);
                    yield return new WaitForFixedUpdate();
                    animator.SetBool(WALK_PARAM, true);
                    Debug.Log($"FastEnemy: Re-triggered walking animation on '{name}'");
                }
            }
        }
    }
    
    // Monitor death animation to ensure it plays
    System.Collections.IEnumerator MonitorDeathAnimation()
    {
        if (animator == null) yield break;
        
        float timeout = 5f; // Max time to wait for death animation
        float elapsed = 0f;
        
        Debug.Log($"FastEnemy: Monitoring death animation on '{name}'");
        
        while (elapsed < timeout)
        {
            if (animator.GetCurrentAnimatorStateInfo(0).IsName("Die") || 
                animator.GetCurrentAnimatorStateInfo(0).IsName("Death"))
            {
                Debug.Log($"FastEnemy: Death animation started on '{name}'");
                yield break; // Animation started successfully
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        Debug.LogWarning($"FastEnemy: Death animation did not start within {timeout} seconds on '{name}'");
    }
    
    // Handle delayed destruction
    System.Collections.IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(Mathf.Max(0f, deathDestroyDelay));
        
        Debug.Log($"FastEnemy: Destroying after delay of {deathDestroyDelay} seconds on '{name}'");
        
        if (gameObject != null)
        {
            Destroy(gameObject);
        }
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
        if (animator != null)
        {
            // Wait for death animation to start
            float waitTime = 0f;
            float maxWaitTime = 1f;
            
            while (waitTime < maxWaitTime)
            {
                var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                if (stateInfo.IsName("Die") || stateInfo.IsName("Death"))
                {
                    Debug.Log($"FastEnemy: Death animation detected, waiting for completion on '{name}'");
                    
                    // Wait for animation to complete (or most of it)
                    while (stateInfo.normalizedTime < 0.95f && animator != null)
                    {
                        yield return new WaitForSeconds(0.1f);
                        if (animator != null)
                            stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                    }
                    break;
                }
                
                waitTime += Time.deltaTime;
                yield return null;
            }
            
            // Additional wait time for death animation (shorter for fast enemy)
            yield return new WaitForSeconds(0.5f);
        }
        else
        {
            // If no animator, just wait a bit
            yield return new WaitForSeconds(1f);
        }
        
        Debug.Log($"FastEnemy: Hiding visuals after death animation on '{name}'");
        
        var renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].enabled = false;
        }

        var canvases = GetComponentsInChildren<Canvas>(true);
        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i] != null)
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
            Debug.Log($"FastEnemy: Starting delayed animator initialization for '{name}'");
            
            // Force animator to update
            animator.Rebind();
            animator.Update(0f);
            
            // Wait a frame for the rebind to take effect
            yield return new WaitForEndOfFrame();
            
            // Check current state
            if (animator.layerCount > 0)
            {
                var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                Debug.Log($"FastEnemy: Current state hash: {stateInfo.shortNameHash}, normalizedTime: {stateInfo.normalizedTime} on '{name}'");
                
                // Try to get state name if possible
                if (animator.runtimeAnimatorController != null)
                {
                    Debug.Log($"FastEnemy: Animator controller has {animator.layerCount} layers");
                }
            }
            
            Debug.Log($"FastEnemy: Setting initial parameter values on '{name}'");
            
            // Set initial parameter values
            if (hasWalkParam)
            {
                animator.SetBool(WALK_PARAM, false);
                Debug.Log($"FastEnemy: Set {WALK_PARAM} to false (initial)");
            }
            
            // Force another update to apply parameter changes
            animator.Update(0f);
            
            // Verify parameter values were set
            if (hasWalkParam)
            {
                bool walkValue = animator.GetBool(WALK_PARAM);
                Debug.Log($"FastEnemy: Verified {WALK_PARAM} = {walkValue}");
            }
            
            Debug.Log($"FastEnemy: Delayed animator initialization completed for '{name}'");
        }
        else
        {
            Debug.LogError($"FastEnemy: Animator is null during delayed initialization on '{name}'");
        }
    }
    
    // Debug method to check animator status - call this from the inspector or console
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void DebugAnimatorStatus()
    {
        if (animator == null)
        {
            Debug.LogError($"FastEnemy: No animator found on '{name}'");
            return;
        }
        
        Debug.Log($"=== ANIMATOR DEBUG for '{name}' ===");
        Debug.Log($"Animator enabled: {animator.enabled}");
        Debug.Log($"Animator gameObject active: {animator.gameObject.activeInHierarchy}");
        Debug.Log($"Has controller: {animator.runtimeAnimatorController != null}");
        Debug.Log($"Has avatar: {animator.avatar != null}");
        Debug.Log($"Animation type: {(animator.isHuman ? "Humanoid" : "Generic")}");
        
        if (animator.runtimeAnimatorController != null)
        {
            Debug.Log($"Controller name: {animator.runtimeAnimatorController.name}");
            Debug.Log($"Layer count: {animator.layerCount}");
            
            if (animator.layerCount > 0)
            {
                var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                Debug.Log($"Current state hash: {stateInfo.shortNameHash}");
                Debug.Log($"Current state normalized time: {stateInfo.normalizedTime}");
                Debug.Log($"Is in transition: {animator.IsInTransition(0)}");
            }
            
            Debug.Log($"Available parameters:");
            foreach (var param in animator.parameters)
            {
                string currentValue = "";
                switch (param.type)
                {
                    case AnimatorControllerParameterType.Bool:
                        currentValue = animator.GetBool(param.name).ToString();
                        break;
                    case AnimatorControllerParameterType.Float:
                        currentValue = animator.GetFloat(param.name).ToString("F2");
                        break;
                    case AnimatorControllerParameterType.Int:
                        currentValue = animator.GetInteger(param.name).ToString();
                        break;
                    case AnimatorControllerParameterType.Trigger:
                        currentValue = "trigger";
                        break;
                }
                Debug.Log($"  {param.name} ({param.type}): {currentValue}");
            }
        }
        
        Debug.Log($"=== END ANIMATOR DEBUG ===");
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
