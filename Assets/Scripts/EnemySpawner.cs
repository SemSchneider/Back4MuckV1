using UnityEngine;
using System.Collections;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject enemyPrefab;
    public int maxEnemies = 5;
    public float spawnRadius = 20f;
    public float spawnInterval = 10f;
    public float minDistanceFromPlayer = 15f;
    
    [Header("Spawn Points")]
    public Transform[] spawnPoints;
    public bool useRandomSpawnPoints = true;
    
    private Transform player;
    private int currentEnemyCount = 0;
    private Coroutine spawnCoroutine;
    
    void Start()
    {
        // Find player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
        
        // Start spawning enemies
        if (enemyPrefab != null)
        {
            // Log prefab animator/controller to catch mismatches early
            var prefabAnimator = enemyPrefab.GetComponentInChildren<Animator>(true);
            string prefabAnimatorName = prefabAnimator != null ? prefabAnimator.name : "<None>";
            string prefabControllerName = (prefabAnimator != null && prefabAnimator.runtimeAnimatorController != null)
                ? prefabAnimator.runtimeAnimatorController.name : "<None>";
            Debug.Log($"EnemySpawner: Using enemyPrefab='{enemyPrefab.name}', Animator='{prefabAnimatorName}', Controller='{prefabControllerName}'");

            spawnCoroutine = StartCoroutine(SpawnEnemies());
        }
        else
        {
            Debug.LogWarning("No enemy prefab assigned to EnemySpawner!");
        }
    }
    
    IEnumerator SpawnEnemies()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);
            
            // Only spawn if we have room for more enemies
            if (currentEnemyCount < maxEnemies)
            {
                SpawnEnemy();
            }
        }
    }
    
    void SpawnEnemy()
    {
        Vector3 spawnPosition = GetSpawnPosition();
        
        if (spawnPosition != Vector3.zero)
        {
            GameObject newEnemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
            
            // Set up enemy reference to this spawner
            SimpleEnemy enemyScript = newEnemy.GetComponent<SimpleEnemy>();
            if (enemyScript != null)
            {
                // Enemy will find player automatically, but we can set it here if needed
            }
            
            // Enforce safe Animator settings and log actual controller on spawned instance
            var enemyAnimator = newEnemy.GetComponentInChildren<Animator>(true);
            if (enemyAnimator != null)
            {
                // Force animator to be enabled and properly configured
                enemyAnimator.enabled = true;
                enemyAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                enemyAnimator.applyRootMotion = false;
                enemyAnimator.speed = 1f;
                
                // Force animator rebind to ensure proper state
                enemyAnimator.Rebind();
                enemyAnimator.Update(0f);
                
                // Set layer weights
                if (enemyAnimator.layerCount > 0)
                {
                    enemyAnimator.SetLayerWeight(0, 1f);
                }

                if (enemyScript != null && enemyScript.animator == null)
                {
                    enemyScript.animator = enemyAnimator; // make reference explicit
                }

                string controllerName = enemyAnimator.runtimeAnimatorController != null
                    ? enemyAnimator.runtimeAnimatorController.name : "<None>";
                Debug.Log($"EnemySpawner: Spawned enemy '{newEnemy.name}' Animator='{enemyAnimator.name}' Controller='{controllerName}'");
                
                // Force initial state to idle
                if (enemyAnimator.runtimeAnimatorController != null)
                {
                    enemyAnimator.Play("Armature|Idle", 0, 0f);
                    Debug.Log($"EnemySpawner: Forced spawned enemy to idle state");
                }
            }
            else
            {
                Debug.LogWarning($"EnemySpawner: Spawned enemy '{newEnemy.name}' has no Animator in children.");
            }
            
            currentEnemyCount++;
            Debug.Log($"Spawned enemy at {spawnPosition}. Total enemies: {currentEnemyCount}");
            
            // Start delayed animator initialization
            StartCoroutine(DelayedAnimatorSetup(newEnemy));
        }
    }
    
    Vector3 GetSpawnPosition()
    {
        Vector3 spawnPos = Vector3.zero;
        int attempts = 0;
        int maxAttempts = 10;
        
        while (attempts < maxAttempts)
        {
            if (useRandomSpawnPoints && spawnPoints != null && spawnPoints.Length > 0)
            {
                // Use predefined spawn points
                Transform randomSpawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
                spawnPos = randomSpawnPoint.position;
            }
            else
            {
                // Generate random position around spawner
                Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
                spawnPos = transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);
            }
            
            // Check if position is far enough from player
            if (player != null)
            {
                float distanceToPlayer = Vector3.Distance(spawnPos, player.position);
                if (distanceToPlayer >= minDistanceFromPlayer)
                {
                    // Check if position is on NavMesh
                    if (UnityEngine.AI.NavMesh.SamplePosition(spawnPos, out UnityEngine.AI.NavMeshHit hit, 5f, UnityEngine.AI.NavMesh.AllAreas))
                    {
                        return hit.position;
                    }
                }
            }
            else
            {
                // No player found, just check NavMesh
                if (UnityEngine.AI.NavMesh.SamplePosition(spawnPos, out UnityEngine.AI.NavMeshHit hit, 5f, UnityEngine.AI.NavMesh.AllAreas))
                {
                    return hit.position;
                }
            }
            
            attempts++;
        }
        
        Debug.LogWarning("Could not find valid spawn position after " + maxAttempts + " attempts");
        return Vector3.zero;
    }
    
    // Called when an enemy dies
    public void OnEnemyDeath()
    {
        currentEnemyCount = Mathf.Max(0, currentEnemyCount - 1);
        Debug.Log($"Enemy died. Remaining enemies: {currentEnemyCount}");
    }
    
    // Visual debugging
    void OnDrawGizmosSelected()
    {
        // Draw spawn radius
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
        
        // Draw minimum distance from player
        if (player != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(player.position, minDistanceFromPlayer);
        }
        
        // Draw spawn points
        if (spawnPoints != null)
        {
            Gizmos.color = Color.blue;
            foreach (Transform spawnPoint in spawnPoints)
            {
                if (spawnPoint != null)
                {
                    Gizmos.DrawWireSphere(spawnPoint.position, 1f);
                    Gizmos.DrawLine(transform.position, spawnPoint.position);
                }
            }
        }
    }
    
    // Delayed animator setup to ensure proper initialization
    IEnumerator DelayedAnimatorSetup(GameObject enemy)
    {
        // Wait a frame for the enemy to fully initialize
        yield return null;
        
        var animator = enemy.GetComponentInChildren<Animator>(true);
        if (animator != null)
        {
            // Force another rebind after initialization
            animator.Rebind();
            animator.Update(0f);
            
            // Ensure it starts in idle state
            animator.Play("Armature|Idle", 0, 0f);
            
            // Test animation parameters
            if (animator.parameters.Length > 0)
            {
                Debug.Log($"EnemySpawner: Delayed setup - Animator has {animator.parameters.Length} parameters");
                foreach (var param in animator.parameters)
                {
                    Debug.Log($"  - {param.name} ({param.type})");
                }
            }
            
            Debug.Log($"EnemySpawner: Delayed animator setup completed for '{enemy.name}'");
        }
    }
    
    void OnDestroy()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
        }
    }
}
