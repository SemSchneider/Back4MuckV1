using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Settings")]
    public GameObject zombiePrefab;
    public float spawnInterval = 10f;

    [Header("Spawn Settings")]
    public Transform[] spawnPoints;
    public int maxZombiesPerSpawnPoint = 10;

    [Header("Day/Night System")]
    public DayNightCycle dayNightCycle;

    private Dictionary<Transform, int> spawnCounts = new Dictionary<Transform, int>();

    void Start()
    {
        foreach (Transform point in spawnPoints)
        {
            spawnCounts[point] = 0;
        }

        StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);
            TrySpawnZombie();
        }
    }

    void TrySpawnZombie()
    {
        // Alleen spawnen als het nacht is
        if (!dayNightCycle.IsNightTime())
            return;

        // Loop door alle spawnpoints tegelijk
        foreach (Transform point in spawnPoints)
        {
            if (spawnCounts[point] < maxZombiesPerSpawnPoint)
            {
                Instantiate(zombiePrefab, point.position, point.rotation);
                spawnCounts[point]++;
            }
        }
    }
}
