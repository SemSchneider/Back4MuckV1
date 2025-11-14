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
            // Maak een GUIStyle aan voor de labels
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 24;                  // Zet hier de gewenste fontgrootte
            style.normal.textColor = Color.white; // Optioneel: tekstkleur
            style.alignment = TextAnchor.UpperLeft;

            // Lege label (optioneel, blijft zoals je had)
            GUI.Label(new Rect(10, 10, 300, 30), "", style);

            if (NightManager.Instance != null)
            {
                GUI.Label(new Rect(10, 0, 300, 40), $"Current Night: {NightManager.Instance.CurrentNight}", style);
            }
            else
            {
                GUI.Label(new Rect(10, 40, 300, 40), "NightManager not found!", style);
            }
        }
    }

}
