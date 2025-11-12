using UnityEngine;

public class NightTriggerHelper : MonoBehaviour
{
    [SerializeField] private bool showOnScreenInstructions = true;

    private void OnGUI()
    {
        if (!showOnScreenInstructions || NightManager.Instance == null) return;

        int currentNight = NightManager.Instance.CurrentNight;
        bool isNight = NightManager.Instance.IsNight;

        GUI.Label(new Rect(50, 50, 400, 30), $"Current Night: {currentNight}");
        GUI.Label(new Rect(50, 80, 400, 30), $"It is currently {(isNight ? "Night" : "Day")}");
    }
}
