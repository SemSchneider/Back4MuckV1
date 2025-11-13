using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Singleton UI system for displaying temporary announcement messages.
/// Shows text with fade-in, hold, and fade-out effects using unscaled time.
/// </summary>
public class AnnouncementUI : MonoBehaviour
{
    // Uncomment the line below to enable verbose announcement UI debugging
    // #define DEBUG_ANNOUNCEMENT_UI
    
    [Header("UI Components")]
    public CanvasGroup canvasGroup;
    public TextMeshProUGUI announcementText;
    
    [Header("Animation Settings")]
    public float fadeInDuration = 0.5f;
    public float defaultHoldDuration = 2f;
    public float fadeOutDuration = 0.5f;
    
    [Header("Text Styling")]
    public Color defaultTextColor = Color.white;
    public float defaultFontSize = 36f;
    
    // Singleton instance
    private static AnnouncementUI _instance;
    public static AnnouncementUI Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<AnnouncementUI>();
                if (_instance == null)
                {
                    Debug.LogError("AnnouncementUI: No instance found in scene! Please add the AnnouncementUI prefab to your scene.");
                }
            }
            return _instance;
        }
    }
    
    // State tracking
    private Coroutine currentAnnouncementCoroutine;
    private bool isShowing = false;
    
    void Awake()
    {
        // Singleton pattern
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAnnouncement();
        }
        else if (_instance != this)
        {
            Debug.LogWarning("AnnouncementUI: Multiple instances detected. Destroying duplicate.");
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        ValidateComponents();
    }
    
    private void InitializeAnnouncement()
    {
        // Ensure the announcement starts hidden
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        
        // Set default text properties
        if (announcementText != null)
        {
            announcementText.color = defaultTextColor;
            announcementText.fontSize = defaultFontSize;
            announcementText.text = "";
        }
        
        Debug.Log("[AnnouncementUI] Initialized and ready");
    }
    
    private void ValidateComponents()
    {
        if (canvasGroup == null)
        {
            Debug.LogError("AnnouncementUI: CanvasGroup reference is missing! Please assign it in the Inspector.");
        }
        
        if (announcementText == null)
        {
            Debug.LogError("AnnouncementUI: TextMeshProUGUI reference is missing! Please assign it in the Inspector.");
        }
    }
    
    /// <summary>
    /// Shows an announcement message with default hold duration.
    /// </summary>
    /// <param name="message">The message to display</param>
    public void Show(string message)
    {
        Show(message, defaultHoldDuration);
    }
    
    /// <summary>
    /// Shows an announcement message with custom hold duration.
    /// </summary>
    /// <param name="message">The message to display</param>
    /// <param name="customHold">How long to hold the message on screen</param>
    public void Show(string message, float customHold)
    {
        if (canvasGroup == null || announcementText == null)
        {
            Debug.LogError("AnnouncementUI: Cannot show announcement - missing components!");
            return;
        }
        
        // Stop any current announcement
        if (currentAnnouncementCoroutine != null)
        {
            StopCoroutine(currentAnnouncementCoroutine);
        }
        
        // Start new announcement
        currentAnnouncementCoroutine = StartCoroutine(ShowAnnouncementCoroutine(message, customHold));
    }
    
    /// <summary>
    /// Shows an announcement with custom styling.
    /// </summary>
    /// <param name="message">The message to display</param>
    /// <param name="customHold">How long to hold the message on screen</param>
    /// <param name="textColor">Custom text color</param>
    /// <param name="fontSize">Custom font size</param>
    public void Show(string message, float customHold, Color textColor, float fontSize)
    {
        if (announcementText != null)
        {
            announcementText.color = textColor;
            announcementText.fontSize = fontSize;
        }
        
        Show(message, customHold);
    }
    
    private IEnumerator ShowAnnouncementCoroutine(string message, float holdDuration)
    {
        isShowing = true;
        
        // Set the message text
        announcementText.text = message;
        
#if DEBUG_ANNOUNCEMENT_UI
        Debug.Log($"[AnnouncementUI] Showing: '{message}' for {holdDuration}s");
#endif
        
        // Fade In
        yield return StartCoroutine(FadeCanvasGroup(0f, 1f, fadeInDuration));
        
        // Hold
        yield return new WaitForSecondsRealtime(holdDuration);
        
        // Fade Out
        yield return StartCoroutine(FadeCanvasGroup(1f, 0f, fadeOutDuration));
        
        // Clear text and reset state
        announcementText.text = "";
        isShowing = false;
        currentAnnouncementCoroutine = null;
        
#if DEBUG_ANNOUNCEMENT_UI
        Debug.Log("[AnnouncementUI] Announcement completed");
#endif
    }
    
    private IEnumerator FadeCanvasGroup(float startAlpha, float endAlpha, float duration)
    {
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float normalizedTime = elapsedTime / duration;
            
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, normalizedTime);
            
            yield return null;
        }
        
        canvasGroup.alpha = endAlpha;
    }
    
    /// <summary>
    /// Immediately hides the current announcement.
    /// </summary>
    public void Hide()
    {
        if (currentAnnouncementCoroutine != null)
        {
            StopCoroutine(currentAnnouncementCoroutine);
            currentAnnouncementCoroutine = null;
        }
        
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
        
        if (announcementText != null)
        {
            announcementText.text = "";
        }
        
        isShowing = false;
        Debug.Log("[AnnouncementUI] Announcement hidden");
    }
    
    /// <summary>
    /// Returns true if an announcement is currently being displayed.
    /// </summary>
    public bool IsShowing()
    {
        return isShowing;
    }
    
    /// <summary>
    /// Quick method for showing success messages (green).
    /// </summary>
    public void ShowSuccess(string message, float holdDuration = -1f)
    {
        float duration = holdDuration > 0 ? holdDuration : defaultHoldDuration;
        Show(message, duration, Color.green, defaultFontSize);
    }
    
    /// <summary>
    /// Quick method for showing warning messages (yellow/orange).
    /// </summary>
    public void ShowWarning(string message, float holdDuration = -1f)
    {
        float duration = holdDuration > 0 ? holdDuration : defaultHoldDuration;
        Show(message, duration, new Color(1f, 0.8f, 0f), defaultFontSize); // Orange
    }
    
    /// <summary>
    /// Quick method for showing danger/error messages (red).
    /// </summary>
    public void ShowDanger(string message, float holdDuration = -1f)
    {
        float duration = holdDuration > 0 ? holdDuration : defaultHoldDuration;
        Show(message, duration, Color.red, defaultFontSize);
    }
    
    /// <summary>
    /// Quick method for showing info messages (cyan).
    /// </summary>
    public void ShowInfo(string message, float holdDuration = -1f)
    {
        float duration = holdDuration > 0 ? holdDuration : defaultHoldDuration;
        Show(message, duration, Color.cyan, defaultFontSize);
    }
    
    // Testing methods for the Inspector
    [ContextMenu("Test Success Message")]
    private void TestSuccess()
    {
        ShowSuccess("Success! This is a test success message.");
    }
    
    [ContextMenu("Test Warning Message")]
    private void TestWarning()
    {
        ShowWarning("Warning! This is a test warning message.");
    }
    
    [ContextMenu("Test Danger Message")]
    private void TestDanger()
    {
        ShowDanger("Danger! This is a test danger message.");
    }
    
    [ContextMenu("Test Info Message")]
    private void TestInfo()
    {
        ShowInfo("Info: This is a test info message.");
    }
}