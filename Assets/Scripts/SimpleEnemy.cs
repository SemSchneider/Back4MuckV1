using UnityEngine;
using UnityEngine.AI;

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
        // Initialize health
        health = Mathf.Clamp(maxHealth, 1f, Mathf.Infinity);

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }

        if (agent == null)
            agent = GetComponent<NavMeshAgent>();
        if (animator == null)
            animator = GetComponent<Animator>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator != null)
        {
            if (forceAnimatorAlwaysAnimate)
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            if (disableAnimatorRootMotion)
                animator.applyRootMotion = false;

            hasWalkParam = animator.AnimatorHasParameter(WALK_PARAM, AnimatorControllerParameterType.Bool);
            hasAttackParam = animator.AnimatorHasParameter(ATTACK_PARAM, AnimatorControllerParameterType.Trigger);
            hasDeathParam = animator.AnimatorHasParameter(DEATH_PARAM, AnimatorControllerParameterType.Trigger);
        }

        if (agent != null)
        {
            agent.speed = moveSpeed;
            agent.stoppingDistance = Mathf.Max(0f, attackRange - 0.5f);
            agent.acceleration = 8f;
            agent.angularSpeed = 120f;

            var capsule = GetComponent<CapsuleCollider>();
            if (capsule != null)
            {
                agent.height = Mathf.Max(agent.height, capsule.height);
                agent.radius = Mathf.Max(agent.radius, capsule.radius);
                agent.baseOffset = Mathf.Max(0f, (capsule.height * 0.5f) - capsule.center.y);
            }

            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                Vector3 snapped = hit.position;
                snapped.y += agent.baseOffset;
                transform.position = snapped;
            }
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    void Update()
    {
        if (isDead) return;
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= detectionRange)
        {
            Vector3 lookDirection = (player.position - transform.position).normalized;
            lookDirection.y = 0;
            if (lookDirection != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(lookDirection);

            if (distanceToPlayer <= attackRange)
            {
                if (agent != null && agent.isActiveAndEnabled)
                {
                    if (stopPushingAtAttackRange)
                        agent.isStopped = true;
                    agent.ResetPath();
                    agent.velocity = Vector3.zero;
                    agent.SetDestination(transform.position);
                }

                if (animator != null && hasWalkParam)
                    animator.SetBool(WALK_PARAM, false);

                if (Time.time - lastAttackTime >= attackCooldown && !isAttacking)
                    Attack();
            }
            else
            {
                if (agent != null && agent.isActiveAndEnabled)
                {
                    agent.isStopped = false;
                    agent.SetDestination(player.position);
                    if (animator != null && hasWalkParam)
                        animator.SetBool(WALK_PARAM, true);
                }
            }
        }
        else
        {
            if (agent != null && agent.isActiveAndEnabled)
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

        if (animator != null && hasAttackParam)
            animator.SetTrigger(ATTACK_PARAM);

        if (player != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            if (distanceToPlayer <= attackRange)
            {
                var playerHealth = player.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                    playerHealth.TakeDamage(attackDamage);
            }
        }

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
        if (health <= 0)
            Die();
    }

    void Die()
    {
        isDead = true;

        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.velocity = Vector3.zero;
            agent.enabled = false;
        }

        if (animator != null && hasDeathParam)
            animator.SetTrigger(DEATH_PARAM);

        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = false;

        if (hideVisualsOnDeath)
        {
            var renderers = GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
                r.enabled = false;

            var canvases = GetComponentsInChildren<Canvas>(true);
            foreach (var c in canvases)
                c.enabled = false;
        }

        // <-- PUNTENSYSTEEM TOEGEVOEGD HIER
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddPoints(10); // 10 punten per kill
        }

        if (deathBehavior == DeathBehavior.Instant)
        {
            Destroy(gameObject);
        }
        else
        {
            Destroy(gameObject, Mathf.Max(0f, deathDestroyDelay));
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

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
        foreach (var p in animator.parameters)
        {
            if (p.type == type && p.name == paramName)
                return true;
        }
        return false;
    }
}
