using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// GameManager singleton that handles game state and win conditions.
/// Manages win scenarios and provides hooks for UI integration.
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("Win Settings")]
    [SerializeField] private bool pauseOnWin = true;
    [SerializeField] private bool showWinUI = true;
    [SerializeField] private string winSceneName = "WinScene";
    [SerializeField] private bool loadWinScene = false;
    
    [Header("Win UI References")]
    [SerializeField] private GameObject winUIPanel;
    [SerializeField] private Text winText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;
    
    [Header("Events")]
    public UnityEvent OnWin;
    
    // Singleton instance
    public static GameManager Instance { get; private set; }
    
    private bool gameWon = false;
    
    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeGameManager();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // Subscribe to SafeZone events
        SafeZone.OnZoneCaptured += WinGame;
        
        // Initialize UI
        if (winUIPanel != null)
        {
            winUIPanel.SetActive(false);
        }
        
        SetupButtons();
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        SafeZone.OnZoneCaptured -= WinGame;
    }
    
    private void InitializeGameManager()
    {
        // Set default win text if not assigned
        if (winText != null && string.IsNullOrEmpty(winText.text))
        {
            winText.text = "You Win!";
        }
    }
    
    private void SetupButtons()
    {
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(RestartGame);
        }
        
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(LoadMainMenu);
        }
    }
    
    /// <summary>
    /// Call this method to trigger the win condition
    /// </summary>
    public void WinGame()
    {
        if (gameWon) return; // Prevent multiple wins
        
        gameWon = true;
        Debug.Log("Game Won!");
        
        // Invoke the UnityEvent for custom UI hooks
        OnWin?.Invoke();
        
        // Handle win based on settings
        if (pauseOnWin)
        {
            Time.timeScale = 0f; // Pause the game
        }
        
        if (loadWinScene && !string.IsNullOrEmpty(winSceneName))
        {
            LoadWinScene();
        }
        else if (showWinUI)
        {
            ShowWinUI();
        }
    }
    
    private void LoadWinScene()
    {
        // Reset time scale before loading scene
        Time.timeScale = 1f;
        
        try
        {
            SceneManager.LoadScene(winSceneName);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load win scene '{winSceneName}': {e.Message}");
            // Fallback to showing UI
            ShowWinUI();
        }
    }
    
    private void ShowWinUI()
    {
        if (winUIPanel != null)
        {
            winUIPanel.SetActive(true);
            Debug.Log("Win UI displayed");
        }
        else
        {
            // Fallback: Just log to console
            Debug.Log("YOU WIN! (No UI panel assigned)");
        }
    }
    
    /// <summary>
    /// Restart the current scene
    /// </summary>
    public void RestartGame()
    {
        Time.timeScale = 1f; // Reset time scale
        gameWon = false; // Reset win state
        
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
    }
    
    /// <summary>
    /// Load the main menu scene
    /// </summary>
    public void LoadMainMenu()
    {
        Time.timeScale = 1f; // Reset time scale
        
        // Try common main menu scene names
        string[] mainMenuNames = { "MainMenu", "Menu", "StartMenu", "Main" };
        
        foreach (string sceneName in mainMenuNames)
        {
            try
            {
                SceneManager.LoadScene(sceneName);
                return;
            }
            catch
            {
                // Continue to next name
            }
        }
        
        // If no main menu found, restart current scene
        Debug.LogWarning("Main menu scene not found. Restarting current scene.");
        RestartGame();
    }
    
    /// <summary>
    /// Reset the game state (useful for testing or multiple rounds)
    /// </summary>
    public void ResetGameState()
    {
        gameWon = false;
        Time.timeScale = 1f;
        
        if (winUIPanel != null)
        {
            winUIPanel.SetActive(false);
        }
        
        Debug.Log("Game state reset");
    }
    
    /// <summary>
    /// Check if the game has been won
    /// </summary>
    public bool IsGameWon()
    {
        return gameWon;
    }
    
    /// <summary>
    /// Pause or unpause the game
    /// </summary>
    public void SetPaused(bool paused)
    {
        Time.timeScale = paused ? 0f : 1f;
    }
    
    /// <summary>
    /// Get the current pause state
    /// </summary>
    public bool IsPaused()
    {
        return Time.timeScale == 0f;
    }
    
    // Static methods for easy access
    public static void Win()
    {
        Instance?.WinGame();
    }
    
    public static void Restart()
    {
        Instance?.RestartGame();
    }
    
    public static void Pause(bool paused)
    {
        Instance?.SetPaused(paused);
    }
}