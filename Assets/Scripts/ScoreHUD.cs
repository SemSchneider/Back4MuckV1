using UnityEngine;
using TMPro;

public class ScoreHUD : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private TextMeshProUGUI scoreText;

    [Header("Debug")]
    [SerializeField] private bool logSubscription = true;

    private void Awake()
    {
        // If you forgot to drag the text, try to find one in children
        if (scoreText == null)
            scoreText = GetComponentInChildren<TextMeshProUGUI>(true);
    }

    private void OnEnable()
    {
        // small delay to give ScoreManager a chance to Awake()
        Invoke(nameof(SubscribeToScoreManager), 0.05f);
    }

    private void OnDisable()
    {
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnScoreChanged.RemoveListener(UpdateScoreUI);
    }

    private void SubscribeToScoreManager()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged.AddListener(UpdateScoreUI);
            UpdateScoreUI(ScoreManager.Instance.GetCurrentPoints());
            if (logSubscription) Debug.Log("ScoreHUD: Subscribed to ScoreManager.OnScoreChanged");
        }
        else
        {
            Debug.LogWarning("ScoreHUD: ScoreManager.Instance was null when trying to subscribe. Make sure ScoreManager exists in the scene.");
        }
    }

    private void UpdateScoreUI(int newScore)
    {
        if (scoreText != null)
        {
            scoreText.text = $"Points: {newScore}";
            if (logSubscription) Debug.Log($"ScoreHUD: Updated UI -> {newScore}");
        }
        else
        {
            Debug.LogWarning("ScoreHUD: scoreText is not assigned or found!");
        }
    }

    // Optional quick test key (remove in production)
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            if (ScoreManager.Instance != null) ScoreManager.Instance.AddPoints(10);
        }
    }
}
