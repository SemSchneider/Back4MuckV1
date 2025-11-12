using UnityEngine;
using System.Collections;

/// <summary>
/// Camera shake system for visual impact effects like taking damage
/// Uses rotation-only shake to avoid interfering with player position
/// </summary>
public class CameraShake : MonoBehaviour
{
    [Header("Shake Settings")]
    public float shakeIntensity = 0.5f;
    public float shakeDuration = 0.3f;
    public AnimationCurve shakeDecay = AnimationCurve.EaseInOut(0, 1, 1, 0);
    
    [Header("Damage Flash Settings")]
    public bool enableDamageFlash = true;
    public Color flashColor = Color.red;
    public float flashDuration = 0.2f;
    public AnimationCurve flashDecay = AnimationCurve.EaseInOut(0, 1, 1, 0);
    
    // Internal state
    private Quaternion originalRotation;
    private bool isShaking = false;
    
    // Flash overlay
    private GameObject flashOverlay;
    private UnityEngine.UI.Image flashImage;
    private Canvas flashCanvas;
    
    // Singleton for easy access
    public static CameraShake Instance { get; private set; }
    
    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // Store original transform
        originalRotation = transform.localRotation;
        
        // Create damage flash overlay
        CreateFlashOverlay();
    }
    
    void CreateFlashOverlay()
    {
        if (!enableDamageFlash) return;
        
        // Create a canvas for the flash overlay
        GameObject canvasGO = new GameObject("DamageFlashCanvas");
        flashCanvas = canvasGO.AddComponent<Canvas>();
        flashCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        flashCanvas.sortingOrder = 1000; // High priority to be on top
        
        // Add CanvasScaler for proper scaling
        var scaler = canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        // Add GraphicRaycaster (required for UI)
        canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        
        // Create the flash image
        flashOverlay = new GameObject("FlashOverlay");
        flashOverlay.transform.SetParent(canvasGO.transform, false);
        
        flashImage = flashOverlay.AddComponent<UnityEngine.UI.Image>();
        flashImage.color = Color.clear; // Start transparent
        flashImage.raycastTarget = false; // Don't block input
        
        // Make it fullscreen
        var rectTransform = flashOverlay.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        
        // Don't destroy on load
        DontDestroyOnLoad(canvasGO);
        
        Debug.Log("CameraShake: Created damage flash overlay");
    }
    
    /// <summary>
    /// Shake the camera with specified intensity and duration (rotation only)
    /// </summary>
    public void Shake(float intensity = -1f, float duration = -1f)
    {
        // Use default values if not specified
        if (intensity < 0) intensity = shakeIntensity;
        if (duration < 0) duration = shakeDuration;
        
        if (!isShaking)
        {
            StartCoroutine(ShakeCoroutine(intensity, duration));
        }
    }
    
    /// <summary>
    /// Trigger damage effects (shake + flash)
    /// </summary>
    public void OnDamageTaken(float damageAmount = 25f)
    {
        // Scale shake intensity based on damage (optional)
        float scaledIntensity = shakeIntensity * Mathf.Clamp(damageAmount / 50f, 0.5f, 2f);
        
        // Start shake
        Shake(scaledIntensity, shakeDuration);
        
        // Start flash
        if (enableDamageFlash && flashImage != null)
        {
            StartCoroutine(FlashCoroutine());
        }
        
        Debug.Log($"CameraShake: Damage effects triggered for {damageAmount} damage");
    }
    
    private IEnumerator ShakeCoroutine(float intensity, float duration)
    {
        isShaking = true;
        float elapsed = 0f;
        
        // Store the current rotation at the start of shake
        Quaternion startRotation = transform.localRotation;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            
            // Calculate shake progress (0 to 1)
            float progress = elapsed / duration;
            
            // Apply decay curve
            float currentIntensity = intensity * shakeDecay.Evaluate(progress);
            
            // Generate random rotation shake only (no position changes)
            Vector3 rotationShake = new Vector3(
                Random.Range(-1f, 1f) * currentIntensity * 2f,
                Random.Range(-1f, 1f) * currentIntensity * 2f,
                Random.Range(-1f, 1f) * currentIntensity * 1f
            );
            
            // Apply shake to rotation only - no position changes
            transform.localRotation = startRotation * Quaternion.Euler(rotationShake);
            
            yield return null;
        }
        
        // Reset to original rotation only
        transform.localRotation = startRotation;
        
        isShaking = false;
    }
    
    private IEnumerator FlashCoroutine()
    {
        if (flashImage == null) yield break;
        
        float elapsed = 0f;
        
        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            
            // Calculate flash progress (0 to 1)
            float progress = elapsed / flashDuration;
            
            // Apply decay curve
            float alpha = flashDecay.Evaluate(progress);
            
            // Apply flash color with calculated alpha
            Color currentColor = flashColor;
            currentColor.a = alpha * 0.3f; // Max 30% opacity
            flashImage.color = currentColor;
            
            yield return null;
        }
        
        // Ensure flash is completely hidden
        flashImage.color = Color.clear;
    }
    
    /// <summary>
    /// Update original rotation if camera parent rotates
    /// </summary>
    public void UpdateOriginalRotation()
    {
        if (!isShaking)
        {
            originalRotation = transform.localRotation;
        }
    }
    
    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
        
        // Clean up flash overlay
        if (flashCanvas != null)
        {
            Destroy(flashCanvas.gameObject);
        }
    }
}