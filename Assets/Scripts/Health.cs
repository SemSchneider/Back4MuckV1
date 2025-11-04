using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;
    private int currentHealth;

    private void Start()
    {
        currentHealth = maxHealth;
    }

    public bool CanHeal => currentHealth < maxHealth;

    public void AddHealth(int amount)
    {
        if (amount < 0)
        {
            Debug.LogWarning($"AddHealth called with negative value ({amount}). Use TakeDamage instead.", this);
            return;
        }

        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
    }

    public void TakeDamage(int amount)
    {
        if (amount < 0)
        {
            Debug.LogWarning($"TakeDamage called with negative value ({amount}). Use AddHealth instead.", this);
            return;
        }

        currentHealth = Mathf.Max(currentHealth - amount, 0);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        // Override in derived class or handle via UnityEvents if needed
        Debug.Log($"{gameObject.name} has died.");
    }

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
}