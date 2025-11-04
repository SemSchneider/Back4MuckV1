using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class DayPickupManager : MonoBehaviour
{
    [System.Serializable]
    public class PickupSpawnInfo
    {
        public GameObject pickupPrefab;
        public int minCount = 1;
        public int maxCount = 3;
        public float minSpawnDistance = 10f;
        public float maxSpawnDistance = 50f;
    }

    [Header("Pickup Settings")]
    [SerializeField] private PickupSpawnInfo healthPickup;
    [SerializeField] private PickupSpawnInfo speedBoostPickup;
    
    [Header("Spawn Settings")]
    [SerializeField] private float checkSpawnInterval = 30f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Vector2 spawnHeightRange = new Vector2(0.5f, 2f);
    
    [Header("References")]
    [SerializeField] private Transform player;
    
    private NightManager nightManager;
    private List<GameObject> activePickups = new List<GameObject>();
    private bool isSpawning = false;

    private void Start()
    {
        nightManager = Object.FindAnyObjectByType<NightManager>();
        if (nightManager == null)
        {
            Debug.LogError("No NightManager found in the scene!", this);
            enabled = false;
            return;
        }

        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
            if (player == null)
            {
                Debug.LogError("No Player found in the scene!", this);
                enabled = false;
                return;
            }
        }

        // Subscribe to day/night events
        nightManager.OnDayStarted.AddListener(OnDayStarted);
        nightManager.OnNightStarted.AddListener(OnNightStarted);

        // If it's already day when we start, begin spawning
        if (!nightManager.IsNight)
        {
            StartSpawning();
        }
    }

    private void OnDayStarted(int nightNumber)
    {
        StartSpawning();
    }

    private void OnNightStarted(int nightNumber)
    {
        StopSpawning();
        CleanupPickups();
    }

    private void StartSpawning()
    {
        if (!isSpawning)
        {
            isSpawning = true;
            StartCoroutine(SpawnRoutine());
        }
    }

    private void StopSpawning()
    {
        isSpawning = false;
        StopAllCoroutines();
    }

    private void CleanupPickups()
    {
        foreach (var pickup in activePickups)
        {
            if (pickup != null)
            {
                var pickupBase = pickup.GetComponent<PickupBase>();
                if (pickupBase != null)
                {
                    pickupBase.Hide();
                }
            }
        }
        activePickups.Clear();
    }

    private IEnumerator SpawnRoutine()
    {
        while (isSpawning)
        {
            SpawnPickups(healthPickup);
            SpawnPickups(speedBoostPickup);
            
            yield return new WaitForSeconds(checkSpawnInterval);
        }
    }

    private void SpawnPickups(PickupSpawnInfo info)
    {
        if (info.pickupPrefab == null) return;

        // Calculate how many pickups to spawn
        int desiredCount = Random.Range(info.minCount, info.maxCount + 1);
        int currentCount = CountActivePickups(info.pickupPrefab);
        int toSpawn = desiredCount - currentCount;

        for (int i = 0; i < toSpawn; i++)
        {
            SpawnPickup(info);
        }
    }

    private int CountActivePickups(GameObject prefab)
    {
        activePickups.RemoveAll(p => p == null);
        return activePickups.Count(p => p.name.StartsWith(prefab.name));
    }

    private void SpawnPickup(PickupSpawnInfo info)
    {
        // Find a spawn position
        Vector3 spawnPos = FindSpawnPosition(info);
        if (spawnPos == Vector3.zero) return;

        // Spawn the pickup
        GameObject pickup = Instantiate(info.pickupPrefab, spawnPos, Quaternion.identity);
        activePickups.Add(pickup);
    }

    private Vector3 FindSpawnPosition(PickupSpawnInfo info)
    {
        for (int attempts = 0; attempts < 30; attempts++)
        {
            // Get random angle and distance from player
            float angle = Random.Range(0f, 360f);
            float distance = Random.Range(info.minSpawnDistance, info.maxSpawnDistance);
            
            // Calculate position
            Vector3 offset = Quaternion.Euler(0, angle, 0) * Vector3.forward * distance;
            Vector3 targetPos = player.position + offset;
            
            // Raycast to find ground
            if (Physics.Raycast(targetPos + Vector3.up * 20f, Vector3.down, out RaycastHit hit, 40f, groundLayer))
            {
                float height = Random.Range(spawnHeightRange.x, spawnHeightRange.y);
                return hit.point + Vector3.up * height;
            }
        }
        
        return Vector3.zero;
    }
}