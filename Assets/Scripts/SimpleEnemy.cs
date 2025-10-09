using UnityEngine;
using UnityEngine.AI;

public class SimpleEnemy : MonoBehaviour
{
    [Header("Enemy Settings")]
    public float health = 100f;
    public float moveSpeed = 3.5f;
    public float detectionRange = 10f;
    public float attackRange = 2f;
    public float attackDamage = 25f;
    public float attackCooldown = 1.5f;
    
    [Header("Components")]
    public Transform player;
    public NavMeshAgent agent;
    public Animator animator;
    
    private float lastAttackTime;
    private bool isDead = false;
    private bool isAttacking = false;
    
    // Animation parameter names
    private const string WALK_PARAM = "IsWalking";
    private const string ATTACK_PARAM = "Attack";
    private const string DEATH_PARAM = "Death";
    
    void Start()
    {
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
            
        // Configure NavMesh Agent
        if (agent != null)
        {
            agent.speed = moveSpeed;
            agent.stoppingDistance = attackRange - 0.5f; // Stop slightly before attack range
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
                    agent.SetDestination(transform.position);
                    
                if (animator != null)
                    animator.SetBool(WALK_PARAM, false);
                
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
                    agent.SetDestination(player.position);
                    if (animator != null)
                        animator.SetBool(WALK_PARAM, true);
                }
            }
        }
        else
        {
            // Player not in range, stop moving
            if (agent != null && agent.isActiveAndEnabled)
                agent.SetDestination(transform.position);
            if (animator != null)
                animator.SetBool(WALK_PARAM, false);
        }
    }
    
    void Attack()
    {
        isAttacking = true;
        lastAttackTime = Time.time;
        
        // Trigger attack animation
        if (animator != null)
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
            agent.enabled = false;
            
        // Trigger death animation
        if (animator != null)
            animator.SetTrigger(DEATH_PARAM);
        
        // Disable collider to prevent further interactions
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = false;
        
        // Destroy after a delay (adjust based on death animation length)
        Destroy(gameObject, 3f);
        
        Debug.Log("Enemy died!");
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
