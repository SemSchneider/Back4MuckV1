using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class NightManager : MonoBehaviour
{
    private void Update()
    {
        // Manual skip for testing
        if (Input.GetKeyDown(KeyCode.N))
        {
            StartNight();
        }
        if (Input.GetKeyDown(KeyCode.M))
        {
            StartDay();
        }
    }
    [Header("Night Settings")]
    [SerializeField] private int currentNight = 1;
    [SerializeField] private bool persistAcrossScenes = true;
    [Header("Cycle Timing")]
    [SerializeField] private float nightDuration = 10f; // seconds
    [SerializeField] private float dayDuration = 5f; // seconds

    private Coroutine cycleCoroutine;

    [Header("Day/Night State")]
    [SerializeField] private bool isNight = true;
    
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

    // Public property for night state
    public bool IsNight => isNight;
    
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
            // Start automatic cycle
            cycleCoroutine = StartCoroutine(RunCycle());
        }
        else
        {
            Destroy(gameObject);
        }
    }
    /// <summary>
    /// Coroutine to loop between day and night using Inspector durations
    /// </summary>
    public IEnumerator RunCycle()
    {
        while (true)
        {
            StartNight();
            yield return new WaitForSeconds(nightDuration);
            StartDay();
            yield return new WaitForSeconds(dayDuration);
        }
    }
    
    /// <summary>
    /// Starts a new night and increments the night counter
    /// </summary>
    public void StartNight()
    {
        isNight = true;
        currentNight++;
        Debug.Log($"Night {currentNight} has started!");
        OnNightStarted?.Invoke(currentNight);
    }

    /// <summary>
    /// Starts a new day and ends the current night
    /// </summary>
    public void StartDay()
    {
        isNight = false;
        Debug.Log($"Day {currentNight} has started!");
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
    /// Reset the night counter to 1 and set to night
    /// </summary>
    public void ResetNight()
    {
        currentNight = 1;
        isNight = true;
        Debug.Log("Night counter reset to 1");
    }
    
    /// <summary>
    /// Get the current night as a formatted string
    /// </summary>
    /// <returns>Formatted night string</returns>
    public string GetNightString()
    {
        return isNight ? $"Night {currentNight}" : $"Day {currentNight}";
    }
    
    /// <summary>
    /// Check if it's currently night time
    /// </summary>
    /// <returns>True if it's night time</returns>
    public bool IsNightTime()
    {
        return isNight;
    }
}
