using UnityEngine;
using TMPro;

[DefaultExecutionOrder(-100)]
public class AmmoManagement : MonoBehaviour
{
    public static AmmoManagement Instance { get; private set; }

    [Header("UI")]
    public TMP_Text ammoText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (ammoText)
        {
            ammoText.enabled = true;
            var c = ammoText.color;
            c.a = 1f;
            ammoText.color = c;
        }
    }

    public void UpdateAmmo(int current, int capacity)
    {
        if (!ammoText) return;
        ammoText.text = current.ToString() + "/" + capacity.ToString();
    }
}
