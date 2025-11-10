using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// SpawnPointGroup that automatically scans children for SpawnPoint components
/// and exposes them as a List<Transform> Points.
/// </summary>
public class SpawnPointGroup : MonoBehaviour
{
    [Header("Spawn Points")]
    [SerializeField] private List<Transform> points = new List<Transform>();
    
    /// <summary>
    /// List of spawn point transforms found in children
    /// </summary>
    public List<Transform> Points => points;
    
    /// <summary>
    /// Number of spawn points found
    /// </summary>
    public int PointCount => points.Count;
    
    private void Awake()
    {
        ScanForSpawnPoints();
    }
    
    /// <summary>
    /// Scans all children for SpawnPoint components and adds their transforms to the points list
    /// </summary>
    private void ScanForSpawnPoints()
    {
        points.Clear();
        
        // Get all SpawnPoint components in children
        SpawnPoint[] spawnPoints = GetComponentsInChildren<SpawnPoint>();
        
        foreach (SpawnPoint spawnPoint in spawnPoints)
        {
            if (spawnPoint.transform != null)
            {
                points.Add(spawnPoint.transform);
            }
        }
        
        Debug.Log($"SpawnPointGroup '{gameObject.name}' found {points.Count} spawn points");
    }
    
    /// <summary>
    /// Get a random spawn point from the list
    /// </summary>
    /// <returns>Random spawn point transform, or null if no points available</returns>
    public Transform GetRandomSpawnPoint()
    {
        if (points.Count == 0)
        {
            Debug.LogWarning($"No spawn points available in {gameObject.name}");
            return null;
        }
        
        return points[Random.Range(0, points.Count)];
    }
    
    /// <summary>
    /// Get a spawn point by index
    /// </summary>
    /// <param name="index">Index of the spawn point</param>
    /// <returns>Spawn point transform, or null if index is invalid</returns>
    public Transform GetSpawnPoint(int index)
    {
        if (index >= 0 && index < points.Count)
        {
            return points[index];
        }
        
        Debug.LogWarning($"Invalid spawn point index {index} in {gameObject.name}");
        return null;
    }
    
    /// <summary>
    /// Manually refresh the spawn points list (useful if spawn points are added at runtime)
    /// </summary>
    public void RefreshSpawnPoints()
    {
        ScanForSpawnPoints();
    }
    
    /// <summary>
    /// Visual debugging - draw gizmos for spawn points
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (points == null) return;
        
        Gizmos.color = Color.green;
        foreach (Transform point in points)
        {
            if (point != null)
            {
                Gizmos.DrawWireSphere(point.position, 0.5f);
                Gizmos.DrawLine(transform.position, point.position);
            }
        }
    }
}
