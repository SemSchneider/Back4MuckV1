using UnityEngine;

/// <summary>
/// Temporary helper script to manually trigger night starts for testing.
/// Press N key to start a new night.
/// </summary>
public class NightTriggerHelper : MonoBehaviour
{
    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private bool showOnScreenInstructions = true;
    
    private void Update()
    {
        // Check for N key press
        if (Input.GetKeyDown(KeyCode.N))
        {
            TriggerNightStart();
        }
    }
    
    /// <summary>
    /// Triggers a night start through NightManager
    /// </summary>
    private void TriggerNightStart()
    {
        if (NightManager.Instance != null)
        {
            int currentNight = NightManager.Instance.CurrentNight;
            LogDebug($"Manually triggering night start. Current night: {currentNight}");
            
            NightManager.Instance.StartNight();
            
            LogDebug($"Night start triggered! New night: {NightManager.Instance.CurrentNight}");
        }
        else
        {
            Debug.LogError("NightTriggerHelper: NightManager.Instance is null! Make sure NightManager is in the scene.");
        }
    }
    
    /// <summary>
    /// Debug logging helper
    /// </summary>
    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"NightTriggerHelper: {message}");
        }
    }
    
    /// <summary>
    /// Display on-screen instructions
    /// </summary>
    private void OnGUI()
    {
        if (showOnScreenInstructions)
        {
            GUI.Label(new Rect(10, 10, 300, 20), "Press N to start next night");
            
            if (NightManager.Instance != null)
            {
                GUI.Label(new Rect(10, 30, 300, 20), $"Current Night: {NightManager.Instance.CurrentNight}");
            }
            else
            {
                GUI.Label(new Rect(10, 30, 300, 20), "NightManager not found!");
            }
        }
    }
}
