using UnityEngine;
using TMPro;

/// <summary>
/// NightHUD displays current night information and enemy counts using TextMeshPro
/// </summary>
public class NightHUD : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI nightNumberText;
    [SerializeField] private TextMeshProUGUI enemyCountText;
    [SerializeField] private TextMeshProUGUI budgetText;
    
    [Header("Display Settings")]
    [SerializeField] private string nightPrefix = "Night ";
    [SerializeField] private string enemyPrefix = "Enemies: ";
    [SerializeField] private string budgetPrefix = "Budget: ";
    [SerializeField] private bool showBudgetInfo = true;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    
    // References
    private EnemySpawnManager spawnManager;
    
    private void Awake()
    {
        // Subscribe to NightManager events
        if (NightManager.Instance != null)
        {
            NightManager.Instance.OnNightStarted.AddListener(OnNightStarted);
            NightManager.Instance.OnDayStarted.AddListener(OnDayStarted);
            LogDebug("Subscribed to NightManager events");
        }
        else
        {
            LogDebug("NightManager.Instance is null - will retry in Start()");
        }
    }
    
    private void Start()
    {
        // Retry NightManager subscription if it wasn't available in Awake
        if (NightManager.Instance != null)
        {
            NightManager.Instance.OnNightStarted.AddListener(OnNightStarted);
            NightManager.Instance.OnDayStarted.AddListener(OnDayStarted);
            LogDebug("Successfully subscribed to NightManager events in Start()");
        }

        // Find EnemySpawnManager
        spawnManager = FindFirstObjectByType<EnemySpawnManager>();
        if (spawnManager == null)
        {
            Debug.LogWarning("NightHUD: EnemySpawnManager not found in scene!");
        }

        // Initialize display with current night
        UpdateNightDisplay();
        UpdateEnemyDisplay();
        UpdateBudgetDisplay();

        LogDebug("NightHUD initialized");
    }
    
    /// <summary>
    /// Called when a new night starts
    /// </summary>
    /// <param name="nightNumber">The night number that started</param>
    private void OnNightStarted(int nightNumber)
    {
        LogDebug($"Night {nightNumber} started - updating HUD");
        UpdateNightDisplay();
        StartCoroutine(UpdateEnemyDisplayDelayed());
    }

    private void OnDayStarted(int nightNumber)
    {
        LogDebug($"Day {nightNumber} started - updating HUD");
        UpdateNightDisplay();
    }
    
    /// <summary>
    /// Coroutine to update enemy display after SpawnManager has computed counts
    /// </summary>
    private System.Collections.IEnumerator UpdateEnemyDisplayDelayed()
    {
        // Wait a frame for SpawnManager to finish its calculations
        yield return null;
        
        // Update enemy and budget displays
        UpdateEnemyDisplay();
        UpdateBudgetDisplay();
        
        LogDebug("Enemy display updated after spawn manager calculations");
    }
    
    /// <summary>
    /// Updates the night number display
    /// </summary>
    private void UpdateNightDisplay()
    {
        if (nightNumberText != null && NightManager.Instance != null)
        {
            string state = NightManager.Instance.IsNight ? "Night" : "Day";
            nightNumberText.text = $"{state} {NightManager.Instance.CurrentNight}";
            LogDebug($"Updated night display: {nightNumberText.text}");
        }
        else if (nightNumberText == null)
        {
            Debug.LogWarning("NightHUD: Night number text field not assigned!");
        }
    }
    
    /// <summary>
    /// Updates the enemy count display
    /// </summary>
    private void UpdateEnemyDisplay()
    {
        if (enemyCountText != null && spawnManager != null)
        {
            spawnManager.GetEnemyCounts(out int alive, out int spawned, out int target);
            
            if (spawnManager.IsUsingBudgetSystem)
            {
                // Budget system: show planned vs spawned
                enemyCountText.text = $"{enemyPrefix}{spawned}/{target} (Alive: {alive})";
            }
            else
            {
                // Legacy system: show spawned vs target
                enemyCountText.text = $"{enemyPrefix}{spawned}/{target} (Alive: {alive})";
            }
            
            LogDebug($"Updated enemy display: {enemyCountText.text}");
        }
        else if (enemyCountText == null)
        {
            Debug.LogWarning("NightHUD: Enemy count text field not assigned!");
        }
    }
    
    /// <summary>
    /// Updates the budget display (only shown for budget system)
    /// </summary>
    private void UpdateBudgetDisplay()
    {
        if (budgetText != null && spawnManager != null && showBudgetInfo)
        {
            if (spawnManager.IsUsingBudgetSystem)
            {
                spawnManager.GetBudgetInfo(out int current, out int remaining, out int cap);
                budgetText.text = $"{budgetPrefix}{remaining}/{current} (Cap: {cap})";
                budgetText.gameObject.SetActive(true);
                LogDebug($"Updated budget display: {budgetText.text}");
            }
            else
            {
                // Hide budget text for legacy system
                budgetText.gameObject.SetActive(false);
            }
        }
        else if (budgetText == null && showBudgetInfo)
        {
            Debug.LogWarning("NightHUD: Budget text field not assigned!");
        }
    }
    
    /// <summary>
    /// Manually refresh all displays (useful for testing or external triggers)
    /// </summary>
    public void RefreshDisplay()
    {
        UpdateNightDisplay();
        UpdateEnemyDisplay();
        UpdateBudgetDisplay();
        LogDebug("Manually refreshed all displays");
    }
    
    /// <summary>
    /// Update enemy counts in real-time (call this when enemies die)
    /// </summary>
    public void UpdateEnemyCounts()
    {
        UpdateEnemyDisplay();
    }
    
    /// <summary>
    /// Set whether to show budget information
    /// </summary>
    public void SetShowBudgetInfo(bool show)
    {
        showBudgetInfo = show;
        UpdateBudgetDisplay();
    }
    
    /// <summary>
    /// Debug logging helper
    /// </summary>
    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"NightHUD: {message}");
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from NightManager events
        if (NightManager.Instance != null)
        {
            NightManager.Instance.OnNightStarted.RemoveListener(OnNightStarted);
        }
    }
}
