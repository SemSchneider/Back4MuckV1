using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// EnemySpawnManager that handles enemy spawning based on night progression.
/// Subscribes to NightManager events and spawns enemies over time at random spawn points.
/// </summary>
public class EnemySpawnManager : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private int baseCount = 3;
    [SerializeField] private int perNightIncrement = 2;
    [SerializeField] private int hardCap = 20;
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private float spawnDelay = 1f;
    
    [Header("Budget System")]
    [SerializeField] private bool useBudgetSystem = false;
    [SerializeField] private int startBudget = 10;
    [SerializeField] private int budgetPerNight = 5;
    [SerializeField] private int budgetCap = 100;
    [SerializeField] private int maxAlive = 15;
    [SerializeField] private EnemyArchetype[] enemyArchetypes;
    
    [Header("Horde Spawning")]
    [SerializeField] private bool spawnAsHordes = true;
    [SerializeField] private int minHordeSize = 3;
    [SerializeField] private int maxHordeSize = 8;
    [SerializeField] private float hordeSpawnDelay = 5f;
    [SerializeField] private float hordeSpawnInterval = 0.2f;
    
    [Header("Curve Settings")]
    [SerializeField] private bool useCurve = false;
    [SerializeField] private AnimationCurve nightMultiplier = AnimationCurve.Linear(1f, 1f, 10f, 2f);
    
    [Header("Spawn Points")]
    [SerializeField] private SpawnPointGroup spawnPointGroup;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    
    // Tracking variables
    private int currentAlive = 0;
    private int spawnedThisNight = 0;
    private int targetSpawnCount = 0;
    private Coroutine spawnCoroutine;
    private bool isSpawning = false;
    
    // Budget tracking
    private int currentBudget = 0;
    private int remainingBudget = 0;
    private List<EnemyArchetype> spawnQueue = new List<EnemyArchetype>();
    
    // Night tracking
    private int currentNight = 0;
    
    private void Awake()
    {
        // Subscribe to NightManager events
        if (NightManager.Instance != null)
        {
            NightManager.Instance.OnNightStarted.AddListener(OnNightStarted);
            LogDebug("Subscribed to NightManager.OnNightStarted");
        }
        else
        {
            LogDebug("NightManager.Instance is null - will retry in Start()");
        }
    }
    
    private void Start()
    {
        // Retry NightManager subscription if it wasn't available in Awake
        if (NightManager.Instance != null && !isSpawning)
        {
            NightManager.Instance.OnNightStarted.AddListener(OnNightStarted);
            LogDebug("Successfully subscribed to NightManager.OnNightStarted in Start()");
        }
        
        // Validate spawn point group
        if (spawnPointGroup == null)
        {
            LogDebug("No SpawnPointGroup assigned - searching for one in scene");
            spawnPointGroup = FindObjectOfType<SpawnPointGroup>();
            
            if (spawnPointGroup == null)
            {
                Debug.LogError("EnemySpawnManager: No SpawnPointGroup found in scene!");
                return;
            }
        }
        
        LogDebug($"EnemySpawnManager initialized with {spawnPointGroup.PointCount} spawn points");
    }
    
    /// <summary>
    /// Called when a new night starts
    /// </summary>
    /// <param name="nightNumber">The night number that started</param>
    private void OnNightStarted(int nightNumber)
    {
        if (isSpawning)
        {
            LogDebug($"Night {nightNumber} started but already spawning - ignoring");
            return;
        }
        
        currentNight = nightNumber;
        
        if (useBudgetSystem)
        {
            // Calculate budget for this night
            int calculatedBudget = startBudget + (nightNumber - 1) * budgetPerNight;
            currentBudget = Mathf.Min(calculatedBudget, budgetCap);
            remainingBudget = currentBudget;
            
            LogDebug($"Night {nightNumber} started - Budget: {currentBudget} (calculated: {calculatedBudget}, capped at {budgetCap})");
            
            // Plan spawns based on budget
            PlanBudgetSpawns();
            
            LogDebug($"Planned {spawnQueue.Count} enemies for {remainingBudget} remaining budget");
        }
        else
        {
            // Calculate how many enemies to spawn this night (legacy system)
            int linearCount = baseCount + (nightNumber - 1) * perNightIncrement;
            
            if (useCurve)
            {
                // Apply curve multiplier
                float multiplier = nightMultiplier.Evaluate(nightNumber);
                targetSpawnCount = Mathf.RoundToInt(linearCount * multiplier);
                LogDebug($"Night {nightNumber} - Linear: {linearCount}, Curve multiplier: {multiplier:F2}, Final: {targetSpawnCount}");
            }
            else
            {
                targetSpawnCount = linearCount;
                LogDebug($"Night {nightNumber} - Linear calculation: {targetSpawnCount}");
            }
            
            // Apply hard cap
            targetSpawnCount = Mathf.Min(targetSpawnCount, hardCap);
            spawnedThisNight = 0;
            
            LogDebug($"Night {nightNumber} started - Target spawn count: {targetSpawnCount} (capped at {hardCap})");
        }
        
        // Start spawning enemies
        StartSpawning();
    }
    
    /// <summary>
    /// Starts the enemy spawning coroutine
    /// </summary>
    private void StartSpawning()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
        }
        
        spawnCoroutine = StartCoroutine(SpawnEnemiesOverTime());
    }
    
    /// <summary>
    /// Coroutine that spawns enemies over time
    /// </summary>
    private IEnumerator SpawnEnemiesOverTime()
    {
        isSpawning = true;
        
        if (useBudgetSystem)
        {
            LogDebug($"Starting to spawn {spawnQueue.Count} enemies from budget queue");
            
            // Initial delay before first spawn
            yield return new WaitForSeconds(spawnDelay);
            
            if (spawnAsHordes)
            {
                // Spawn enemies in hordes
                yield return StartCoroutine(SpawnHordes());
            }
            else
            {
                // Spawn enemies individually
                yield return StartCoroutine(SpawnIndividually());
            }
        }
        else
        {
            LogDebug($"Starting to spawn {targetSpawnCount} enemies over time");
            
            // Initial delay before first spawn
            yield return new WaitForSeconds(spawnDelay);
            
            // Legacy spawning system with maxAlive respect
            while (spawnedThisNight < targetSpawnCount)
            {
                // Wait if we're at max alive limit
                while (currentAlive >= maxAlive)
                {
                    LogDebug($"At max alive limit ({maxAlive}), waiting for slots to open...");
                    yield return new WaitForSeconds(0.5f); // Check every 0.5 seconds
                }
                
                // Check if we have valid spawn points
                if (spawnPointGroup == null || spawnPointGroup.PointCount == 0)
                {
                    Debug.LogError("EnemySpawnManager: No valid spawn points available!");
                    break;
                }
                
                // Spawn an enemy
                SpawnEnemy();
                
                // Wait for next spawn
                yield return new WaitForSeconds(spawnInterval);
            }
        }
        
        isSpawning = false;
        LogDebug($"Finished spawning enemies for night {currentNight}");
    }
    
    /// <summary>
    /// Spawns enemies in hordes
    /// </summary>
    private IEnumerator SpawnHordes()
    {
        int hordeNumber = 1;
        int enemiesInCurrentHorde = 0;
        int currentHordeSize = 0;
        
        for (int i = 0; i < spawnQueue.Count; i++)
        {
            // Check if we need to start a new horde
            if (enemiesInCurrentHorde == 0)
            {
                // Calculate horde size based on remaining enemies
                int remainingEnemies = spawnQueue.Count - i;
                currentHordeSize = Mathf.Min(Random.Range(minHordeSize, maxHordeSize + 1), remainingEnemies);
                
                LogDebug($"Starting Horde {hordeNumber} with {currentHordeSize} enemies");
                
                // Wait between hordes (except for first horde)
                if (hordeNumber > 1)
                {
                    yield return new WaitForSeconds(hordeSpawnDelay);
                }
            }
            
            // Wait if we're at max alive limit
            while (currentAlive >= maxAlive)
            {
                LogDebug($"At max alive limit ({maxAlive}), waiting for slots to open...");
                yield return new WaitForSeconds(0.5f);
            }
            
            // Check if we have valid spawn points
            if (spawnPointGroup == null || spawnPointGroup.PointCount == 0)
            {
                Debug.LogError("EnemySpawnManager: No valid spawn points available!");
                break;
            }
            
            // Spawn the enemy
            SpawnArchetype(spawnQueue[i]);
            enemiesInCurrentHorde++;
            
            // Wait between enemies in the same horde
            yield return new WaitForSeconds(hordeSpawnInterval);
            
            // Check if horde is complete
            if (enemiesInCurrentHorde >= currentHordeSize)
            {
                LogDebug($"Horde {hordeNumber} complete! Spawned {enemiesInCurrentHorde} enemies");
                hordeNumber++;
                enemiesInCurrentHorde = 0;
            }
        }
        
        LogDebug($"All hordes spawned for night {currentNight}");
    }
    
    /// <summary>
    /// Spawns enemies individually (legacy behavior)
    /// </summary>
    private IEnumerator SpawnIndividually()
    {
        for (int i = 0; i < spawnQueue.Count; i++)
        {
            // Wait if we're at max alive limit
            while (currentAlive >= maxAlive)
            {
                LogDebug($"At max alive limit ({maxAlive}), waiting for slots to open...");
                yield return new WaitForSeconds(0.5f);
            }
            
            // Check if we have valid spawn points
            if (spawnPointGroup == null || spawnPointGroup.PointCount == 0)
            {
                Debug.LogError("EnemySpawnManager: No valid spawn points available!");
                break;
            }
            
            // Spawn the archetype
            SpawnArchetype(spawnQueue[i]);
            
            // Wait for next spawn
            yield return new WaitForSeconds(spawnInterval);
        }
    }
    
    /// <summary>
    /// Plans enemy spawns based on budget constraints
    /// </summary>
    private void PlanBudgetSpawns()
    {
        spawnQueue.Clear();
        
        // Check if we have valid archetypes first
        var validArchetypes = enemyArchetypes?.Where(a => a != null && a.EnemyPrefab != null).ToArray();
        
        if (validArchetypes == null || validArchetypes.Length == 0)
        {
            // No valid archetypes - use fallback system
            if (enemyPrefab != null)
            {
                LogDebug("No valid archetypes found, using simple enemy prefab as fallback");
                // Create a temporary spawn queue with just the simple enemy
                spawnQueue.Clear();
                int fallbackBudgetUsed = 0;
                int fallbackAttempts = 0;
                int fallbackMaxAttempts = currentBudget * 2;
                
                while (fallbackBudgetUsed < currentBudget && fallbackAttempts < fallbackMaxAttempts)
                {
                    int remainingBudget = currentBudget - fallbackBudgetUsed;
                    if (remainingBudget >= 1) // Simple enemy costs 1
                    {
                        spawnQueue.Add(null); // null represents simple enemy
                        fallbackBudgetUsed += 1;
                    }
                    else
                    {
                        break;
                    }
                    fallbackAttempts++;
                }
                
                LogDebug($"Fallback planning complete: {spawnQueue.Count} simple enemies planned");
                return;
            }
            else
            {
                Debug.LogError("EnemySpawnManager: No valid enemy archetypes found and no fallback enemy prefab!");
                return;
            }
        }
        
        int budgetUsed = 0;
        int attempts = 0;
        int maxAttempts = currentBudget * 2; // Prevent infinite loops
        
        while (budgetUsed < currentBudget && attempts < maxAttempts)
        {
            // Get available archetypes that fit remaining budget
            int remainingBudget = currentBudget - budgetUsed;
            var affordableArchetypes = validArchetypes.Where(a => a.Cost <= remainingBudget).ToArray();
            
            if (affordableArchetypes.Length == 0)
            {
                LogDebug($"No more affordable archetypes. Budget used: {budgetUsed}/{currentBudget}");
                break;
            }
            
            // Weighted random selection
            EnemyArchetype selectedArchetype = SelectWeightedArchetype(affordableArchetypes);
            
            if (selectedArchetype != null)
            {
                spawnQueue.Add(selectedArchetype);
                budgetUsed += selectedArchetype.Cost;
                LogDebug($"Selected {selectedArchetype.ArchetypeName} (cost: {selectedArchetype.Cost}), total budget used: {budgetUsed}");
            }
            
            attempts++;
        }
        
        LogDebug($"Budget planning complete: {spawnQueue.Count} enemies planned, {budgetUsed}/{currentBudget} budget used");
        
        // Organize enemies into hordes if horde spawning is enabled
        if (spawnAsHordes && spawnQueue.Count > 0)
        {
            OrganizeIntoHordes();
        }
    }
    
    /// <summary>
    /// Organizes planned enemies into hordes for spawning
    /// </summary>
    private void OrganizeIntoHordes()
    {
        if (spawnQueue.Count == 0) return;
        
        List<EnemyArchetype> originalQueue = new List<EnemyArchetype>(spawnQueue);
        spawnQueue.Clear();
        
        int remainingEnemies = originalQueue.Count;
        int hordeNumber = 1;
        
        while (remainingEnemies > 0)
        {
            // Calculate horde size
            int hordeSize = Mathf.Min(Random.Range(minHordeSize, maxHordeSize + 1), remainingEnemies);
            
            LogDebug($"Creating Horde {hordeNumber} with {hordeSize} enemies");
            
            // Add enemies to this horde
            for (int i = 0; i < hordeSize; i++)
            {
                if (originalQueue.Count > 0)
                {
                    spawnQueue.Add(originalQueue[0]);
                    originalQueue.RemoveAt(0);
                    remainingEnemies--;
                }
            }
            
            hordeNumber++;
        }
        
        LogDebug($"Organized enemies into {hordeNumber - 1} hordes");
    }
    
    /// <summary>
    /// Selects an archetype using weighted random selection
    /// </summary>
    private EnemyArchetype SelectWeightedArchetype(EnemyArchetype[] archetypes)
    {
        if (archetypes.Length == 0) return null;
        if (archetypes.Length == 1) return archetypes[0];
        
        // Calculate total weight
        float totalWeight = archetypes.Sum(a => a.SpawnWeight);
        
        // Random selection
        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;
        
        foreach (var archetype in archetypes)
        {
            currentWeight += archetype.SpawnWeight;
            if (randomValue <= currentWeight)
            {
                return archetype;
            }
        }
        
        // Fallback to last archetype
        return archetypes[archetypes.Length - 1];
    }
    
    /// <summary>
    /// Spawns an enemy archetype at a random spawn point
    /// </summary>
    private void SpawnArchetype(EnemyArchetype archetype)
    {
        GameObject prefabToSpawn = null;
        string enemyName = "Unknown";
        
        if (archetype == null)
        {
            // Fallback to simple enemy prefab
            if (enemyPrefab == null)
            {
                Debug.LogError("EnemySpawnManager: No fallback enemy prefab available!");
                return;
            }
            prefabToSpawn = enemyPrefab;
            enemyName = "SimpleEnemy";
        }
        else if (archetype.EnemyPrefab == null)
        {
            Debug.LogError("EnemySpawnManager: Invalid archetype prefab!");
            return;
        }
        else
        {
            prefabToSpawn = archetype.EnemyPrefab;
            enemyName = archetype.ArchetypeName;
        }
        
        Transform spawnPoint = spawnPointGroup.GetRandomSpawnPoint();
        if (spawnPoint == null)
        {
            Debug.LogError("EnemySpawnManager: Failed to get random spawn point!");
            return;
        }
        
        // Spawn the enemy
        GameObject newEnemy = Instantiate(prefabToSpawn, spawnPoint.position, spawnPoint.rotation);
        
        // Set up enemy reference to this spawn manager
        SimpleEnemy enemyScript = newEnemy.GetComponent<SimpleEnemy>();
        if (enemyScript != null)
        {
            LogDebug($"Spawned {enemyName} '{newEnemy.name}' at {spawnPoint.position}");
        }
        else
        {
            Debug.LogWarning($"Spawned {enemyName} '{newEnemy.name}' but no SimpleEnemy component found!");
        }
        
        // Update tracking
        spawnedThisNight++;
        currentAlive++;
        
        LogDebug($"Spawned {enemyName} {spawnedThisNight}/{spawnQueue.Count}. Alive: {currentAlive}");
    }
    
    /// <summary>
    /// Spawns a single enemy at a random spawn point (legacy method)
    /// </summary>
    private void SpawnEnemy()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("EnemySpawnManager: No enemy prefab assigned!");
            return;
        }
        
        Transform spawnPoint = spawnPointGroup.GetRandomSpawnPoint();
        if (spawnPoint == null)
        {
            Debug.LogError("EnemySpawnManager: Failed to get random spawn point!");
            return;
        }
        
        // Spawn the enemy
        GameObject newEnemy = Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);
        
        // Set up enemy reference to this spawn manager
        SimpleEnemy enemyScript = newEnemy.GetComponent<SimpleEnemy>();
        if (enemyScript != null)
        {
            // You can add any enemy setup here if needed
            LogDebug($"Spawned enemy '{newEnemy.name}' at {spawnPoint.position}");
        }
        else
        {
            Debug.LogWarning($"Spawned enemy '{newEnemy.name}' but no SimpleEnemy component found!");
        }
        
        // Update tracking
        spawnedThisNight++;
        currentAlive++;
        
        LogDebug($"Spawned enemy {spawnedThisNight}/{targetSpawnCount}. Alive: {currentAlive}");
    }
    
    /// <summary>
    /// Register an enemy death - called by enemies when they die
    /// </summary>
    public void RegisterDeath()
    {
        currentAlive = Mathf.Max(0, currentAlive - 1);
        LogDebug($"Enemy died. Current alive: {currentAlive}");
        
        // Notify HUD of count change
        NotifyHUDOfCountChange();
        
        // Optional: Check if all enemies are dead and trigger events
        if (currentAlive == 0 && !isSpawning)
        {
            LogDebug("All enemies defeated for this night!");
            // You can add night completion logic here
        }
    }
    
    /// <summary>
    /// Notify HUD components of count changes
    /// </summary>
    private void NotifyHUDOfCountChange()
    {
        NightHUD hud = FindObjectOfType<NightHUD>();
        if (hud != null)
        {
            hud.UpdateEnemyCounts();
        }
    }
    
    /// <summary>
    /// Get current enemy count information
    /// </summary>
    public void GetEnemyCounts(out int alive, out int spawned, out int target)
    {
        alive = currentAlive;
        spawned = spawnedThisNight;
        target = useBudgetSystem ? spawnQueue.Count : targetSpawnCount;
    }
    
    /// <summary>
    /// Get current budget information
    /// </summary>
    public void GetBudgetInfo(out int current, out int remaining, out int cap)
    {
        current = currentBudget;
        remaining = remainingBudget;
        cap = budgetCap;
    }
    
    /// <summary>
    /// Check if budget system is enabled
    /// </summary>
    public bool IsUsingBudgetSystem => useBudgetSystem;
    
    /// <summary>
    /// Force stop spawning (useful for debugging or emergency stops)
    /// </summary>
    public void StopSpawning()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
        
        isSpawning = false;
        LogDebug("Spawning stopped manually");
    }
    
    /// <summary>
    /// Reset the spawn manager state
    /// </summary>
    public void ResetState()
    {
        StopSpawning();
        currentAlive = 0;
        spawnedThisNight = 0;
        targetSpawnCount = 0;
        currentNight = 0;
        
        // Reset budget system
        currentBudget = 0;
        remainingBudget = 0;
        spawnQueue.Clear();
        
        LogDebug("Spawn manager state reset");
    }
    
    /// <summary>
    /// Preview spawn counts for multiple nights (useful for curve testing)
    /// </summary>
    [ContextMenu("Preview Spawn Counts")]
    public void PreviewSpawnCounts()
    {
        Debug.Log("=== Enemy Spawn Preview ===");
        
        if (useBudgetSystem)
        {
            Debug.Log($"Budget System: Start Budget: {startBudget}, Budget Per Night: {budgetPerNight}, Budget Cap: {budgetCap}, Max Alive: {maxAlive}");
            Debug.Log($"Horde Spawning: {(spawnAsHordes ? "Enabled" : "Disabled")}");
            if (spawnAsHordes)
            {
                Debug.Log($"Horde Settings: Min Size: {minHordeSize}, Max Size: {maxHordeSize}, Delay: {hordeSpawnDelay}s, Interval: {hordeSpawnInterval}s");
            }
            Debug.Log($"Archetypes: {enemyArchetypes?.Length ?? 0} assigned");
            
            for (int night = 1; night <= 10; night++)
            {
                int calculatedBudget = startBudget + (night - 1) * budgetPerNight;
                int actualBudget = Mathf.Min(calculatedBudget, budgetCap);
                string capInfo = calculatedBudget > budgetCap ? $" (capped from {calculatedBudget})" : "";
                
                if (spawnAsHordes)
                {
                    int estimatedHordes = Mathf.CeilToInt((float)actualBudget / ((minHordeSize + maxHordeSize) / 2));
                    Debug.Log($"Night {night}: Budget = {actualBudget}{capInfo} (~{estimatedHordes} hordes)");
                }
                else
                {
                    Debug.Log($"Night {night}: Budget = {actualBudget}{capInfo}");
                }
            }
        }
        else
        {
            Debug.Log($"Base Count: {baseCount}, Per Night Increment: {perNightIncrement}, Hard Cap: {hardCap}");
            Debug.Log($"Use Curve: {useCurve}");
            
            for (int night = 1; night <= 10; night++)
            {
                int linearCount = baseCount + (night - 1) * perNightIncrement;
                int finalCount = linearCount;
                
                if (useCurve)
                {
                    float multiplier = nightMultiplier.Evaluate(night);
                    finalCount = Mathf.RoundToInt(linearCount * multiplier);
                }
                
                finalCount = Mathf.Min(finalCount, hardCap);
                
                string curveInfo = useCurve ? $" (Linear: {linearCount}, Multiplier: {nightMultiplier.Evaluate(night):F2})" : "";
                Debug.Log($"Night {night}: {finalCount}{curveInfo}");
            }
        }
    }
    
    /// <summary>
    /// Debug logging helper
    /// </summary>
    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"EnemySpawnManager: {message}");
        }
    }
    
    /// <summary>
    /// Visual debugging - draw spawn points
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (spawnPointGroup != null && spawnPointGroup.Points != null)
        {
            Gizmos.color = Color.red;
            foreach (Transform point in spawnPointGroup.Points)
            {
                if (point != null)
                {
                    Gizmos.DrawWireSphere(point.position, 1f);
                }
            }
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from NightManager events
        if (NightManager.Instance != null)
        {
            NightManager.Instance.OnNightStarted.RemoveListener(OnNightStarted);
        }
        
        // Stop any running coroutines
        StopSpawning();
    }
}
