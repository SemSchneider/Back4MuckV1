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
        SubscribeToNightManager();
    }

    private void Start()
    {
        // Find EnemySpawnManager
        spawnManager = FindFirstObjectByType<EnemySpawnManager>();
        if (spawnManager == null)
        {
            Debug.LogWarning("NightHUD: EnemySpawnManager not found in scene!");
        }

        // Initialize display with current night
        UpdateAllDisplays();
        LogDebug("NightHUD initialized");
    }

    private void Update()
    {
        // Realtime night update (in case scene starts during night)
        UpdateNightDisplay();
    }

    private void SubscribeToNightManager()
    {
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

    private void OnNightStarted(int nightNumber)
    {
        LogDebug($"Night {nightNumber} started - updating HUD");
        UpdateNightDisplay();
        StartCoroutine(UpdateEnemyDisplayDelayed());
    }

    /// <summary>
    /// Update enemy counts in real-time (call this when enemies die)
    /// </summary>
    public void UpdateEnemyCounts()
    {
        UpdateEnemyDisplay();
    }

    private void OnDayStarted(int nightNumber)
    {
        LogDebug($"Day {nightNumber} started - updating HUD");
        UpdateNightDisplay();
    }

    private System.Collections.IEnumerator UpdateEnemyDisplayDelayed()
    {
        yield return null; // wait one frame
        UpdateEnemyDisplay();
        UpdateBudgetDisplay();
        LogDebug("Enemy and budget display updated after night start");
    }

    private void UpdateNightDisplay()
    {
        if (nightNumberText != null && NightManager.Instance != null)
        {
            string state = NightManager.Instance.IsNight ? "Night" : "Day";
            nightNumberText.text = $"{state} {NightManager.Instance.CurrentNight}";
            LogDebug($"Updated night display: {nightNumberText.text}");
        }
    }

    private void UpdateEnemyDisplay()
    {
        if (enemyCountText != null && spawnManager != null)
        {
            spawnManager.GetEnemyCounts(out int alive, out int spawned, out int target);
            enemyCountText.text = $"{enemyPrefix}{spawned}/{target} (Alive: {alive})";
            LogDebug($"Updated enemy display: {enemyCountText.text}");
        }
    }

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
                budgetText.gameObject.SetActive(false);
            }
        }
    }

    private void UpdateAllDisplays()
    {
        UpdateNightDisplay();
        UpdateEnemyDisplay();
        UpdateBudgetDisplay();
    }

    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"NightHUD: {message}");
        }
    }

    private void OnDestroy()
    {
        if (NightManager.Instance != null)
        {
            NightManager.Instance.OnNightStarted.RemoveListener(OnNightStarted);
            NightManager.Instance.OnDayStarted.RemoveListener(OnDayStarted);
        }
    }
}
