using UnityEngine;

/// <summary>
/// ScriptableObject that defines an enemy archetype with prefab and cost for budget-based spawning
/// </summary>
[CreateAssetMenu(fileName = "New Enemy Archetype", menuName = "Enemy/Enemy Archetype")]
public class EnemyArchetype : ScriptableObject
{
    [Header("Archetype Settings")]
    [SerializeField] private string archetypeName = "Basic Enemy";
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private int cost = 1;
    [SerializeField] private float spawnWeight = 1f;
    
    [Header("Description")]
    [TextArea(3, 5)]
    [SerializeField] private string description = "Basic enemy archetype";
    
    /// <summary>
    /// Name of this archetype
    /// </summary>
    public string ArchetypeName => archetypeName;
    
    /// <summary>
    /// Prefab to spawn for this archetype
    /// </summary>
    public GameObject EnemyPrefab => enemyPrefab;
    
    /// <summary>
    /// Cost of spawning this archetype
    /// </summary>
    public int Cost => cost;
    
    /// <summary>
    /// Weight for random selection (higher = more likely to be chosen)
    /// </summary>
    public float SpawnWeight => spawnWeight;
    
    /// <summary>
    /// Description of this archetype
    /// </summary>
    public string Description => description;
    
    /// <summary>
    /// Validate the archetype settings
    /// </summary>
    private void OnValidate()
    {
        if (enemyPrefab == null)
        {
            Debug.LogWarning($"EnemyArchetype '{archetypeName}': No enemy prefab assigned!");
        }
        
        if (cost <= 0)
        {
            Debug.LogWarning($"EnemyArchetype '{archetypeName}': Cost must be greater than 0!");
        }
        
        if (spawnWeight <= 0.001f) // Use small epsilon instead of 0
        {
            Debug.LogWarning($"EnemyArchetype '{archetypeName}': Spawn weight ({spawnWeight}) must be greater than 0!");
        }
        
        // Debug log to see actual values
        Debug.Log($"EnemyArchetype '{archetypeName}' validation: Cost={cost}, SpawnWeight={spawnWeight}");
    }
}
