using UnityEngine;
using UnityEngine.Events;

public class NightManager : MonoBehaviour
{
    [Header("Night Settings")]
    [SerializeField] private int currentNight = 1;
    [SerializeField] private bool persistAcrossScenes = true;
    
    [Header("Events")]
    public UnityEvent<int> OnNightStarted;
    public UnityEvent<int> OnDayStarted;
    
    // Singleton instance
    public static NightManager Instance { get; private set; }
    
    // Public property for current night
    public int CurrentNight 
    { 
        get => currentNight; 
        private set => currentNight = value; 
    }
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            
            // Optional persistence across scenes
            if (persistAcrossScenes)
            {
                DontDestroyOnLoad(gameObject);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Starts a new night and increments the night counter
    /// </summary>
    public void StartNight()
    {
        currentNight++;
        Debug.Log($"Night {currentNight} has started!");
        
        // Invoke the night started event
        OnNightStarted?.Invoke(currentNight);
    }
    
    /// <summary>
    /// Ends the current night and starts a new day
    /// </summary>
    public void EndNight()
    {
        Debug.Log($"Night {currentNight} has ended! Day {currentNight} begins.");
        
        // Invoke the day started event
        OnDayStarted?.Invoke(currentNight);
    }
    
    /// <summary>
    /// Manually set the current night (useful for testing or save/load)
    /// </summary>
    /// <param name="nightNumber">The night number to set</param>
    public void SetNight(int nightNumber)
    {
        if (nightNumber > 0)
        {
            currentNight = nightNumber;
            Debug.Log($"Night manually set to {currentNight}");
        }
        else
        {
            Debug.LogWarning("Night number must be greater than 0!");
        }
    }
    
    /// <summary>
    /// Reset the night counter to 1
    /// </summary>
    public void ResetNight()
    {
        currentNight = 1;
        Debug.Log("Night counter reset to 1");
    }
    
    /// <summary>
    /// Get the current night as a formatted string
    /// </summary>
    /// <returns>Formatted night string</returns>
    public string GetNightString()
    {
        return $"Night {currentNight}";
    }
    
    /// <summary>
    /// Check if it's currently night time (you can extend this with time-based logic)
    /// </summary>
    /// <returns>True if it's night time</returns>
    public bool IsNightTime()
    {
        // This is a placeholder - you can implement time-based logic here
        // For example, check against a day/night cycle system
        return true; // Default to night time for now
    }
}
