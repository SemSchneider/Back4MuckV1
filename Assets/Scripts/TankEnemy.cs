using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System;

/// <summary>
/// Tank Enemy Archetype - High health, slow movement, high damage, longer range, charge attack
/// </summary>
public class TankEnemy : MonoBehaviour
{
    #region Events
    
    // Static event for when any tank enemy dies
    public static event Action<GameObject> OnEnemyDied;
    
    #endregion

    [Header("Tank Enemy Settings")]
    public float maxHealth = 300f;  // Higher health (increased from 250f)
    public float health = 300f;
    public float moveSpeed = 1.2f;  // Slower than SimpleEnemy (reduced from 1.5f)
    public float detectionRange = 18f;  // Longer detection range (increased)
    public float attackRange = 4.5f;  // Longer attack range (increased)
    public float attackDamage = 60f;  // Higher damage (increased)
    public float attackCooldown = 2.8f;  // Slower attacks (increased)
    public bool stopPushingAtAttackRange = true;

    [Header("Tank Special Abilities")]
    public float chargeSpeed = 8f;  // Speed during charge attack (increased)
    public float chargeDistance = 10f;  // Maximum charge distance (increased)
    public float chargeDamage = 100f;  // Damage during charge (increased)
    public float chargeRecoveryTime = 2.5f;  // Time to recover after charge (reduced)
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
    
    [Header("Feedback Effects")]
    public GameObject bloodHitEffectPrefab;
    public Transform hitEffectSpawnPoint; // Optional: specific point to spawn effects, defaults to center of enemy
    public bool enableHitFeedback = true;
    public float hitFeedbackDuration = 0.4f; // Longer for tank enemy
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
    private const string CHARGE_PARAM = "Charge";
    private const string HIT_PARAM = "Hit";
    
    void Start()
    {
        // Initialize health from maxHealth at start
        health = Mathf.Clamp(maxHealth, 1f, Mathf.Infinity);
        
        // Find player if not assigned
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                Debug.Log($"TankEnemy: Found player '{playerObj.name}'");
            }
            else
            {
                Debug.LogWarning($"TankEnemy: No GameObject with 'Player' tag found! Make sure your player has the 'Player' tag.");
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
                
                // Check if the model has an avatar
                if (animator.avatar == null)
                {
                    Debug.LogWarning($"TankEnemy: No Avatar assigned to animator on '{name}'. This may cause animation issues. Consider setting Animation Type to 'Generic' in the model import settings.");
                    
                    // For models without avatars, ensure Generic animation type
                    if (animator.isHuman)
                    {
                        Debug.LogError($"TankEnemy: Animator is set to Humanoid but no Avatar is assigned on '{name}'. Change the model's Animation Type to 'Generic' in import settings.");
                    }
                    else
                    {
                        Debug.Log($"TankEnemy: Using Generic animation type (recommended for models without avatars) on '{name}'");
                    }
                }
                else
                {
                    Debug.Log($"TankEnemy: Avatar '{animator.avatar.name}' assigned, animation type: {(animator.isHuman ? "Humanoid" : "Generic")} on '{name}'");
                }
                
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
            hasHitParam = TankEnemyAnimatorExtensions.AnimatorHasParameter(animator, HIT_PARAM, AnimatorControllerParameterType.Trigger);

            // Debug all available parameters
            Debug.Log($"TankEnemy: Checking animator controller '{animator.runtimeAnimatorController.name}' on '{name}'");
            var allParams = animator.parameters;
            Debug.Log($"TankEnemy: Found {allParams.Length} parameters:");
            for (int i = 0; i < allParams.Length; i++)
            {
                var param = allParams[i];
                Debug.Log($"  - {param.name} ({param.type})");
            }

            // Log parameter status for debugging
            Debug.Log($"TankEnemy: Parameter mapping - Walk:{hasWalkParam}, Attack:{hasAttackParam}, Death:{hasDeathParam}, Charge:{hasChargeParam}, Hit:{hasHitParam}");
            
            if (!hasWalkParam)
                Debug.LogWarning($"TankEnemy: Animator missing Bool parameter '{WALK_PARAM}' on '{name}'.");
            if (!hasAttackParam)
                Debug.LogWarning($"TankEnemy: Animator missing Trigger parameter '{ATTACK_PARAM}' on '{name}'.");
            if (!hasDeathParam)
                Debug.LogWarning($"TankEnemy: Animator missing Trigger parameter '{DEATH_PARAM}' on '{name}'.");
            if (!hasChargeParam && canCharge)
                Debug.LogWarning($"TankEnemy: Animator missing Trigger parameter '{CHARGE_PARAM}' on '{name}'. Charge attacks disabled.");
            if (!hasHitParam && enableHitFeedback)
                Debug.LogWarning($"TankEnemy: Animator missing Trigger parameter '{HIT_PARAM}' on '{name}'. Hit animation won't play.");
        }
        
        // Initialize hit feedback materials and renderers
        if (enableHitFeedback)
        {
            InitializeHitFeedback();
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
        
        if (player == null) 
        {
            Debug.LogWarning($"TankEnemy '{name}': Player reference is null! Cannot function.");
            return;
        }
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        // Continuously ensure walking animation loops while moving (like SimpleEnemy)
        if (animator != null && hasWalkParam && animator.GetBool(WALK_PARAM))
        {
            EnsureWalkingAnimationLoops();
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
                {
                    animator.SetBool(WALK_PARAM, false);
                    Debug.Log($"TankEnemy: Set {WALK_PARAM}=false (in attack range) on '{name}'");
                }
                
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
                    
                    // Simple walking animation like SimpleEnemy
                    if (animator != null && hasWalkParam)
                    {
                        animator.SetBool(WALK_PARAM, true);
                        Debug.Log($"TankEnemy: Set {WALK_PARAM}=true on '{name}'");
                        
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
            if (agent != null && agent.isActiveAndEnabled && !isCharging)
            {
                agent.isStopped = true;
                agent.SetDestination(transform.position);
                agent.velocity = Vector3.zero;
            }
            if (animator != null && hasWalkParam)
            {
                animator.SetBool(WALK_PARAM, false);
                Debug.Log($"TankEnemy: Set {WALK_PARAM}=false (out of range) on '{name}'");
            }
        }
    }
    
    void Attack()
    {
        isAttacking = true;
        lastAttackTime = Time.time;
        
        Debug.Log($"TankEnemy: Performing heavy attack on '{name}'");
        
        // Trigger attack animation
        if (animator != null && hasAttackParam)
        {
            animator.SetTrigger(ATTACK_PARAM);
            Debug.Log($"TankEnemy: Triggered {ATTACK_PARAM} animation on '{name}'");
        }
        else if (animator != null)
        {
            Debug.LogWarning($"TankEnemy: Cannot trigger {ATTACK_PARAM} - parameter not found on '{name}'");
        }
        
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
        
        // Trigger hit feedback
        if (enableHitFeedback)
        {
            TriggerHitFeedback();
        }
        
        if (health <= 0)
        {
            Debug.Log($"Tank Enemy died after taking {damage} damage");
            Die();
        }
    }
    
    private void InitializeHitFeedback()
    {
        // Get all renderers on this enemy
        enemyRenderers = GetComponentsInChildren<Renderer>();
        if (enemyRenderers.Length == 0)
        {
            Debug.LogWarning($"TankEnemy: No renderers found for hit feedback on '{name}'");
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
            Destroy(bloodEffect, 7f); // Longer for tank enemy
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
        
        // Wait for the feedback duration (longer for tank enemy)
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
    
    public void Die()
    {
        if (isDead) return; // Prevent multiple death calls
        
        isDead = true;
        isCharging = false;
        isRecovering = false;
        
        // Notify that this tank enemy has died
        OnEnemyDied?.Invoke(gameObject);
        
        Debug.Log($"TankEnemy: Starting death sequence for '{name}'");
        
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
                Debug.Log($"TankEnemy: Set {WALK_PARAM}=false for death on '{name}'");
            }
            
            if (hasDeathParam)
            {
                animator.SetTrigger(DEATH_PARAM);
                Debug.Log($"TankEnemy: Triggered {DEATH_PARAM} animation on '{name}'");
                
                // Force animator update to process the trigger
                animator.Update(0f);
                
                // Check if death animation started
                StartCoroutine(MonitorDeathAnimation());
            }
            else
            {
                Debug.LogWarning($"TankEnemy: Cannot trigger {DEATH_PARAM} - parameter not found on '{name}'");
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
            Debug.Log($"TankEnemy: Instant death - destroying immediately on '{name}'");
            Destroy(gameObject);
        }
        else
        {
            Debug.Log($"TankEnemy: Timed death - will destroy in {deathDestroyDelay} seconds on '{name}'");
            
            // Hide visuals after death animation
            if (hideVisualsOnDeath)
            {
                StartCoroutine(HideAfterDeathAnimation());
            }
            
            // Destroy after delay
            StartCoroutine(DestroyAfterDelay());
        }

        Debug.Log($"TankEnemy: Death sequence initiated for '{name}'");
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
                    Debug.Log($"TankEnemy: Reset walking animation to loop on '{name}'");
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
                Debug.Log($"TankEnemy: Walking animation verified on '{name}' - State: {stateInfo.shortNameHash}, Time: {stateInfo.normalizedTime:F2}");
            }
            else
            {
                Debug.LogWarning($"TankEnemy: Walking animation may not be playing correctly on '{name}' - State: {stateInfo.shortNameHash}, Time: {stateInfo.normalizedTime:F2}");
                
                // Try to force the walking state again
                if (hasWalkParam && agent != null && agent.velocity.magnitude > 0.1f)
                {
                    animator.SetBool(WALK_PARAM, false);
                    yield return new WaitForFixedUpdate();
                    animator.SetBool(WALK_PARAM, true);
                    Debug.Log($"TankEnemy: Re-triggered walking animation on '{name}'");
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
        
        Debug.Log($"TankEnemy: Monitoring death animation on '{name}'");
        
        while (elapsed < timeout)
        {
            if (animator.GetCurrentAnimatorStateInfo(0).IsName("Die") || 
                animator.GetCurrentAnimatorStateInfo(0).IsName("Death"))
            {
                Debug.Log($"TankEnemy: Death animation started on '{name}'");
                yield break; // Animation started successfully
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        Debug.LogWarning($"TankEnemy: Death animation did not start within {timeout} seconds on '{name}'");
    }
    
    // Handle delayed destruction
    System.Collections.IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(Mathf.Max(0f, deathDestroyDelay));
        
        Debug.Log($"TankEnemy: Destroying after delay of {deathDestroyDelay} seconds on '{name}'");
        
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
                    Debug.Log($"TankEnemy: Death animation detected, waiting for completion on '{name}'");
                    
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
            
            // Additional wait time for death animation
            yield return new WaitForSeconds(1f);
        }
        else
        {
            // If no animator, just wait a bit
            yield return new WaitForSeconds(2f);
        }
        
        Debug.Log($"TankEnemy: Hiding visuals after death animation on '{name}'");
        
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
            Debug.Log($"TankEnemy: Starting delayed animator initialization for '{name}'");
            
            // Force animator to update
            animator.Rebind();
            animator.Update(0f);
            
            // Wait a frame for the rebind to take effect
            yield return new WaitForEndOfFrame();
            
            // Check current state
            if (animator.layerCount > 0)
            {
                var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                Debug.Log($"TankEnemy: Current state hash: {stateInfo.shortNameHash}, normalizedTime: {stateInfo.normalizedTime} on '{name}'");
                
                // Try to get state name if possible
                if (animator.runtimeAnimatorController != null)
                {
                    Debug.Log($"TankEnemy: Animator controller has {animator.layerCount} layers");
                }
            }
            
            Debug.Log($"TankEnemy: Setting initial parameter values on '{name}'");
            
            // Set initial parameter values
            if (hasWalkParam)
            {
                animator.SetBool(WALK_PARAM, false);
                Debug.Log($"TankEnemy: Set {WALK_PARAM} to false (initial)");
            }
            
            // Force another update to apply parameter changes
            animator.Update(0f);
            
            // Verify parameter values were set
            if (hasWalkParam)
            {
                bool walkValue = animator.GetBool(WALK_PARAM);
                Debug.Log($"TankEnemy: Verified {WALK_PARAM} = {walkValue}");
            }
            
            Debug.Log($"TankEnemy: Delayed animator initialization completed for '{name}'");
        }
        else
        {
            Debug.LogError($"TankEnemy: Animator is null during delayed initialization on '{name}'");
        }
    }
    
    // Debug method to check animator status - call this from the inspector or console
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void DebugAnimatorStatus()
    {
        if (animator == null)
        {
            Debug.LogError($"TankEnemy: No animator found on '{name}'");
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
