using TMPro;
using UnityEngine;

public class ScoreHUD : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreText;

    private void OnEnable()
    {
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnScoreChanged.AddListener(UpdateScoreUI);
    }

    private void OnDisable()
    {
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnScoreChanged.RemoveListener(UpdateScoreUI);
    }

    private void Start()
    {
        // Initialize display
        if (ScoreManager.Instance != null)
            UpdateScoreUI(ScoreManager.Instance.GetCurrentPoints());
    }

    private void UpdateScoreUI(int newScore)
    {
        if (scoreText != null)
        {
            scoreText.text = $"Points: {newScore}";
            Debug.Log($"HUD updated: {newScore}"); // Voor debug
        }
    }
}
