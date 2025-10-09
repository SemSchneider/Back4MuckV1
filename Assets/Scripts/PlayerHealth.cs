using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;
    
    [Header("UI")]
    public Slider healthBar;
    public TMP_Text healthText;
    
    [Header("Death Settings")]
    public GameObject deathScreen;
    public float respawnDelay = 3f;
    
    private bool isDead = false;
    private bool warnedMissingHealthBar = false;
    
    void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthUI();
        
        // Hide death screen if it exists
        if (deathScreen != null)
            deathScreen.SetActive(false);

        // Runtime guidance: warn clearly if UI bindings are missing
        if (healthBar == null)
        {
            Debug.LogWarning("PlayerHealth: 'healthBar' is not assigned. Add a Slider on your HUD Canvas and assign it to PlayerHealth.healthBar.", this);
        }
        if (healthText == null)
        {
            Debug.Log("PlayerHealth: 'healthText' is not assigned (optional). Assign a TMP_Text if you want numeric health.", this);
        }
    }
    
    public void TakeDamage(float damage)
    {
        if (isDead) return;
        
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        
        UpdateHealthUI();
        
        Debug.Log($"Player took {damage} damage. Health: {currentHealth:F1}/{maxHealth:F1}");
        
        if (currentHealth <= 0)
        {
            Debug.Log($"Player died after taking {damage} damage");
            Die();
        }
    }
    
    public void Heal(float amount)
    {
        if (isDead) return;
        
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        
        UpdateHealthUI();
        
        Debug.Log($"Player healed for {amount}. Health: {currentHealth:F1}/{maxHealth:F1}");
    }
    
    void UpdateHealthUI()
    {
        // Update health bar
        if (healthBar != null)
        {
            healthBar.value = currentHealth / maxHealth;
        }
        else if (!warnedMissingHealthBar)
        {
            warnedMissingHealthBar = true;
            Debug.LogWarning("PlayerHealth: healthBar Slider reference is missing. The health UI will not update until it is assigned.", this);
        }
        
        // Update health text
        if (healthText != null)
        {
            healthText.text = $"{currentHealth:F0}/{maxHealth:F0}";
        }
    }
    
    void Die()
    {
        if (isDead) return;
        
        isDead = true;
        Debug.Log("Player died!");
        
        // Show death screen
        if (deathScreen != null)
            deathScreen.SetActive(true);
        
        // Disable player movement
        var mouseMovement = GetComponent<MouseMovement>();
        if (mouseMovement != null)
            mouseMovement.enabled = false;
            
        var playerMovement = GetComponent<PlayerMovement>();
        if (playerMovement != null)
            playerMovement.enabled = false;
        
        // Disable weapon
        var inventory = GetComponent<Inventory>();
        if (inventory != null)
            inventory.enabled = false;
        
        // Schedule respawn
        Invoke(nameof(Respawn), respawnDelay);
    }
    
    void Respawn()
    {
        isDead = false;
        currentHealth = maxHealth;
        UpdateHealthUI();
        
        // Hide death screen
        if (deathScreen != null)
            deathScreen.SetActive(false);
        
        // Re-enable player movement
        var mouseMovement = GetComponent<MouseMovement>();
        if (mouseMovement != null)
            mouseMovement.enabled = true;
            
        var playerMovement = GetComponent<PlayerMovement>();
        if (playerMovement != null)
            playerMovement.enabled = true;
        
        // Re-enable weapon
        var inventory = GetComponent<Inventory>();
        if (inventory != null)
            inventory.enabled = true;
        
        Debug.Log("Player respawned!");
    }
    
    // For testing purposes - can be called from UI buttons
    [ContextMenu("Take 25 Damage")]
    public void TestTakeDamage()
    {
        TakeDamage(25f);
    }
    
    [ContextMenu("Heal 25")]
    public void TestHeal()
    {
        Heal(25f);
    }
}
