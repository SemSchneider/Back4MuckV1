using UnityEngine;
using UnityEngine.Events;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Score Settings")]
    [SerializeField] private int currentPoints = 0;

    [Header("Events")]
    public UnityEvent<int> OnScoreChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Voeg punten toe bij een kill of andere acties
    /// </summary>
    public void AddPoints(int amount)
    {
        currentPoints += amount;
        OnScoreChanged?.Invoke(currentPoints);
        Debug.Log($"Score updated: {currentPoints}");
    }

    /// <summary>
    /// Probeer punten uit te geven voor powerups
    /// </summary>
    public bool SpendPoints(int amount)
    {
        if (currentPoints >= amount)
        {
            currentPoints -= amount;
            OnScoreChanged?.Invoke(currentPoints);
            Debug.Log($"Spent {amount} points. Remaining: {currentPoints}");
            return true;
        }
        else
        {
            Debug.Log("Not enough points!");
            return false;
        }
    }

    public int GetCurrentPoints()
    {
        return currentPoints;
    }
}
