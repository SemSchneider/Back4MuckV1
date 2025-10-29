using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    [Header("References")]
    public SimpleEnemy enemy;
    public Slider slider;
    public Transform pivot; // optional, the transform that holds the canvas above enemy

    [Header("Billboard")]
    public bool faceCamera = true;
    public Camera targetCamera;

    void Awake()
    {
        if (enemy == null)
        {
            enemy = GetComponentInParent<SimpleEnemy>();
        }
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    void LateUpdate()
    {
        if (enemy == null || slider == null) return;

        float normalized = 1f;
        if (enemy.maxHealth > 0f)
        {
            normalized = Mathf.Clamp01(enemy.health / enemy.maxHealth);
        }
        slider.value = normalized;

        if (faceCamera && targetCamera != null)
        {
            Transform t = pivot != null ? pivot : transform;
            // Billboard toward camera
            Vector3 camForward = targetCamera.transform.forward;
            camForward.y = 0f;
            if (camForward.sqrMagnitude > 0.0001f)
            {
                t.rotation = Quaternion.LookRotation(camForward, Vector3.up);
            }
        }
    }
}


