using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>
/// SafeZone script that creates a protected area for the player.
/// Features:
/// - Heals player over time when inside
/// - Captures the zone when player is alone (no enemies)
/// - Tracks capture progress and triggers win condition
/// </summary>
public class SafeZone : MonoBehaviour
{
    // Uncomment the line below to enable verbose safe zone debugging
    // #define DEBUG_SAFE_ZONE
    
    [Header("Zone Settings")]
    public float healPerSecond = 5f;
    public float capturePerSecond = 10f;
    public float captureMax = 100f;
    public float decayPerSecond = 5f;
    
    [Header("References")]
    public Slider captureSlider;
    public string playerTag = "Player";
    public string enemyTag = "Enemy";
    
    [Header("Visual Effects")]
    [SerializeField] private GameObject visualRing;
    [SerializeField] private Color safeZoneColor = Color.green;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float minAlpha = 0.3f;
    [SerializeField] private float maxAlpha = 0.8f;
    
    // Tracking variables
    private bool playerInside = false;
    private HashSet<GameObject> enemiesInside = new HashSet<GameObject>();
    private float captureProgress = 0f;
    
    // Cached references
    private PlayerHealth playerHealth;
    private Renderer ringRenderer;
    private Material ringMaterial;
    private bool zoneCompleted = false;
    
    // Events
    public static event Action OnZoneCaptured;
    
    // Helper method to safely show announcements
    private void ShowAnnouncement(string message, float duration = 2f, string type = "info")
    {
        if (AnnouncementUI.Instance != null)
        {
            switch (type.ToLower())
            {
                case "success":
                    AnnouncementUI.Instance.ShowSuccess(message, duration);
                    break;
                case "warning":
                    AnnouncementUI.Instance.ShowWarning(message, duration);
                    break;
                case "danger":
                    AnnouncementUI.Instance.ShowDanger(message, duration);
                    break;
                default:
                    AnnouncementUI.Instance.ShowInfo(message, duration);
                    break;
            }
        }
        else
        {
            Debug.LogWarning($"[SafeZone] AnnouncementUI not available. Message: {message}");
        }
    }
    
    void Start()
    {
        SetupSafeZone();
        UpdateCaptureUI();
        
        // Subscribe to all enemy death events
        BaseEnemy.OnEnemyDied += OnEnemyDied;
        TankEnemy.OnEnemyDied += OnEnemyDied;
        FastEnemy.OnEnemyDied += OnEnemyDied;
        SimpleEnemy.OnEnemyDied += OnEnemyDied;
    }
    
    void OnDestroy()
    {
        // Unsubscribe from all enemy death events
        BaseEnemy.OnEnemyDied -= OnEnemyDied;
        TankEnemy.OnEnemyDied -= OnEnemyDied;
        FastEnemy.OnEnemyDied -= OnEnemyDied;
        SimpleEnemy.OnEnemyDied -= OnEnemyDied;
    }
    
    void Update()
    {
        if (visualRing != null && ringRenderer != null)
        {
            AnimateRing();
        }
        
        // Heal player if inside
        if (playerInside && playerHealth != null)
        {
            playerHealth.Heal(healPerSecond * Time.deltaTime);
        }
        
        // Handle zone capture
        if (!zoneCompleted)
        {
            if (playerInside && enemiesInside.Count == 0)
            {
                // Slow capture progress when player is inside (safer but slower)
                captureProgress += decayPerSecond * Time.deltaTime;  // Use slower decay rate
                captureProgress = Mathf.Clamp(captureProgress, 0f, captureMax);
                
#if DEBUG_SAFE_ZONE
                Debug.Log($"[SafeZone] 🐌 Player inside & safe! Progress: {captureProgress:F1}/{captureMax} (+{decayPerSecond}/sec)");
#endif
                UpdateCaptureUI();
                
                // Check if zone is captured
                if (captureProgress >= captureMax)
                {
                    CompleteZone();
                }
            }
            else if (!playerInside)
            {
                // Fast capture progress when player is outside (risky but faster)
                captureProgress += capturePerSecond * Time.deltaTime;  // Use faster capture rate
                captureProgress = Mathf.Clamp(captureProgress, 0f, captureMax);
                
#if DEBUG_SAFE_ZONE
                Debug.Log($"[SafeZone] 🏃‍♂️ Player outside! Fast progress: {captureProgress:F1}/{captureMax} (+{capturePerSecond}/sec)");
#endif
                UpdateCaptureUI();
                
                // Check if zone is captured
                if (captureProgress >= captureMax)
                {
                    CompleteZone();
                }
            }
            else
            {
                // Player inside but enemies present, pause (no increase or decrease)
#if DEBUG_SAFE_ZONE
                Debug.Log($"[SafeZone] ⏸️ Player inside but {enemiesInside.Count} enemies present! Progress paused at: {captureProgress:F1}/{captureMax}");
#endif
            }
        }
    }
    
    private void SetupSafeZone()
    {
        // Setup visual ring
        if (visualRing != null)
        {
            // Get the collider radius to match visual ring size
            SphereCollider sphereCol = GetComponent<SphereCollider>();
            if (sphereCol != null)
            {
                // Scale the visual ring to match the collider radius
                // The plane primitive is 10x10 units, so scale = radius * 2 / 10
                float ringScale = sphereCol.radius * 2f / 10f;
                visualRing.transform.localScale = new Vector3(ringScale, ringScale, 1f);
                
                Debug.Log($"[SafeZone] Visual ring scaled to {ringScale} to match collider radius {sphereCol.radius}");
            }
            
            ringRenderer = visualRing.GetComponent<Renderer>();
            if (ringRenderer != null)
            {
                // Create a material instance to avoid modifying the shared material
                ringMaterial = new Material(ringRenderer.material);
                ringRenderer.material = ringMaterial;
                
                // Set the color to green with transparency
                safeZoneColor = new Color(0f, 1f, 0f, 0.5f); // Green with 50% alpha
                ringMaterial.color = safeZoneColor;
                
                // Ensure material is set up for transparency
                ringMaterial.SetFloat("_Mode", 2f); // Set to Transparent mode
                ringMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                ringMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                ringMaterial.SetInt("_ZWrite", 0);
                ringMaterial.DisableKeyword("_ALPHATEST_ON");
                ringMaterial.EnableKeyword("_ALPHABLEND_ON");
                ringMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                ringMaterial.renderQueue = 3000;
                
                Debug.Log("[SafeZone] Visual ring material configured as green transparent");
            }
            else
            {
                Debug.LogWarning("[SafeZone] Visual ring has no Renderer component!");
            }
        }
        else
        {
            Debug.LogWarning("[SafeZone] Visual ring reference is missing!");
        }
    }
    
    private void AnimateRing()
    {
        if (ringMaterial != null)
        {
            float alpha = Mathf.Lerp(minAlpha, maxAlpha, 
                (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f);
            
            Color color = ringMaterial.color;
            color.a = alpha;
            ringMaterial.color = color;
        }
    }
    
    private void UpdateCaptureUI()
    {
        if (captureSlider != null)
        {
            captureSlider.value = captureProgress / captureMax;
        }
    }
    
    private void CompleteZone()
    {
        zoneCompleted = true;
        Debug.Log("Safe Zone Captured!");
        
        // Show completion announcement
        ShowAnnouncement("Safe Zone Captured! - Victory!", 3f, "success");
        
        // Raise the event
        OnZoneCaptured?.Invoke();
        
        // Notify GameManager directly
        if (GameManager.Instance != null)
        {
            GameManager.Instance.WinGame();
            Debug.Log("Notifying GameManager of zone capture");
        }
        else
        {
            Debug.LogWarning("GameManager not found! Cannot notify of zone capture.");
        }
        
        // Visual feedback for completion
        if (ringMaterial != null)
        {
            ringMaterial.color = Color.yellow; // Change to yellow when completed
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
#if DEBUG_SAFE_ZONE
        Debug.Log($"[SafeZone] Something entered trigger: '{other.name}' with tag '{other.tag}' and layer '{LayerMask.LayerToName(other.gameObject.layer)}'");
#endif
        
        if (other.CompareTag(playerTag))
        {
            playerInside = true;
            playerHealth = other.GetComponent<PlayerHealth>();
            
#if DEBUG_SAFE_ZONE
            Debug.Log($"[SafeZone] ✅ PLAYER ENTERED safe zone! PlayerHealth found: {playerHealth != null}");
#endif
            
            // Show announcement
            ShowAnnouncement("Entered Safe Zone - Healing & Slower Capture", 2f, "success");
        }
        else if (other.CompareTag(enemyTag))
        {
            enemiesInside.Add(other.gameObject);
            Debug.Log($"[SafeZone] Enemy '{other.name}' entered safe zone. Enemies inside: {enemiesInside.Count}");
            
            // Show contest message if player is inside
            if (playerInside && enemiesInside.Count == 1) // First enemy to enter while player is inside
            {
                ShowAnnouncement("Zombies in Safe Zone - Progress Paused", 2f, "warning");
            }
        }
        else
        {
            Debug.Log($"[SafeZone] ❌ Object '{other.name}' entered but doesn't match Player ('{playerTag}') or Enemy ('{enemyTag}') tags");
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        Debug.Log($"[SafeZone] Something exited trigger: '{other.name}' with tag '{other.tag}'");
        
        if (other.CompareTag(playerTag))
        {
            playerInside = false;
            playerHealth = null;
            
            Debug.Log("[SafeZone] ❌ PLAYER LEFT safe zone");
            
            // Show announcement
            ShowAnnouncement("Left Safe Zone - No Healing / Faster Capture", 2f, "danger");
        }
        else if (other.CompareTag(enemyTag))
        {
            enemiesInside.Remove(other.gameObject);
            Debug.Log($"[SafeZone] Enemy '{other.name}' left safe zone. Enemies inside: {enemiesInside.Count}");
            
            // Show zone clear message if player is inside and this was the last enemy
            if (playerInside && enemiesInside.Count == 0)
            {
                ShowAnnouncement("Zone Clear — Capture Resumed", 2f, "success");
            }
        }
    }
    
    /// <summary>
    /// Called when any enemy in the game dies. Removes dead enemies from the zone tracking.
    /// </summary>
    private void OnEnemyDied(GameObject deadEnemy)
    {
        if (enemiesInside.Contains(deadEnemy))
        {
            enemiesInside.Remove(deadEnemy);
            Debug.Log($"Dead enemy {deadEnemy.name} removed from safe zone tracking. Enemies inside: {enemiesInside.Count}");
        }
    }
    
    // Public methods for external access
    public bool IsPlayerInside()
    {
        return playerInside;
    }
    
    public int GetEnemyCount()
    {
        return enemiesInside.Count;
    }
    
    public float GetCaptureProgress()
    {
        return captureProgress;
    }
    
    public float GetCapturePercentage()
    {
        return (captureProgress / captureMax) * 100f;
    }
    
    public bool IsZoneCompleted()
    {
        return zoneCompleted;
    }
    
    // Draw gizmos in the editor
    void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = new Color(safeZoneColor.r, safeZoneColor.g, safeZoneColor.b, 0.3f);
            
            if (col is SphereCollider sphereCol)
            {
                Gizmos.DrawSphere(transform.position, sphereCol.radius);
                Gizmos.color = safeZoneColor;
                Gizmos.DrawWireSphere(transform.position, sphereCol.radius);
            }
            else if (col is BoxCollider boxCol)
            {
                Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.localScale);
                Gizmos.DrawCube(Vector3.zero, boxCol.size);
                Gizmos.color = safeZoneColor;
                Gizmos.DrawWireCube(Vector3.zero, boxCol.size);
            }
        }
    }
    
    /// <summary>
    /// Updates the visual ring to match the current collider radius.
    /// Call this if you change the collider radius at runtime.
    /// </summary>
    [ContextMenu("Update Visual Ring Size")]
    public void UpdateVisualRingSize()
    {
        if (visualRing != null)
        {
            SphereCollider sphereCol = GetComponent<SphereCollider>();
            if (sphereCol != null)
            {
                float ringScale = sphereCol.radius * 2f / 10f;
                visualRing.transform.localScale = new Vector3(ringScale, ringScale, 1f);
                Debug.Log($"[SafeZone] Visual ring updated to scale {ringScale} for radius {sphereCol.radius}");
            }
        }
    }
    
    /// <summary>
    /// Forces the material to be green and transparent.
    /// Call this if the material appears wrong.
    /// </summary>
    [ContextMenu("Fix Material Color")]
    public void FixMaterialColor()
    {
        if (ringMaterial != null)
        {
            safeZoneColor = new Color(0f, 1f, 0f, 0.5f);
            ringMaterial.color = safeZoneColor;
            Debug.Log("[SafeZone] Material color forced to green");
        }
        else
        {
            Debug.LogWarning("[SafeZone] No ring material found to fix");
        }
    }
}