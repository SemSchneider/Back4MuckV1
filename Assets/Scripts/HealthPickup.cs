using UnityEngine;

public class HealthPickup : PickupBase
{
    [SerializeField] private int healAmount = 25;

    protected override bool Collect(GameObject collector)
    {
        var health = collector.GetComponent<PlayerHealth>();
        if (health == null)
        {
            Debug.LogWarning($"Player {collector.name} tried to collect {gameObject.name} but has no PlayerHealth component!", collector);
            return false;
        }

        if (health.currentHealth >= health.maxHealth)
        {
            return false;
        }

        health.Heal(healAmount);
        return true;
    }
}