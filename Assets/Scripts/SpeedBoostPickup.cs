using UnityEngine;

public class SpeedBoostPickup : PickupBase
{
    [Header("Speed Boost Settings")]
    [SerializeField] private float multiplier = 1.5f;
    [SerializeField] private float duration = 5f;

    protected override bool Collect(GameObject collector)
    {
        var playerMovement = collector.GetComponent<PlayerMovement>();
        if (playerMovement == null)
        {
            Debug.LogWarning($"Player {collector.name} tried to collect {gameObject.name} but has no PlayerMovement component!", collector);
            return false;
        }

        playerMovement.ApplySpeedMultiplier(multiplier, duration);
        return true;
    }
}