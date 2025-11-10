using UnityEngine;
using System.Collections;

public class PickupBase : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Collider trigger;
    [SerializeField] private Renderer[] visuals;

    [Header("Settings")]
    [SerializeField] private float respawnTime = 30f;

    public float RespawnTime => respawnTime;

    private void Awake()
    {
        // Auto-get collider from root if not set
        if (trigger == null)
        {
            trigger = GetComponent<Collider>();
            if (trigger == null)
            {
                Debug.LogError($"No Collider found on {gameObject.name}. Pickups require a trigger Collider!", this);
            }
            else if (!trigger.isTrigger)
            {
                Debug.LogWarning($"Collider on {gameObject.name} is not set as trigger!", this);
            }
        }

        // Auto-collect renderers from Model child if not set
        if (visuals == null || visuals.Length == 0)
        {
            Transform modelTransform = transform.Find("Model");
            if (modelTransform != null)
            {
                visuals = modelTransform.GetComponentsInChildren<Renderer>();
                if (visuals.Length == 0)
                {
                    Debug.LogWarning($"No Renderers found under Model child of {gameObject.name}!", this);
                }
            }
            else
            {
                Debug.LogError($"No Model child found on {gameObject.name}. Pickup prefabs should have a Model child containing visuals!", this);
            }
        }
    }

    public void Show()
    {
        if (trigger != null)
        {
            trigger.enabled = true;
        }

        if (visuals != null)
        {
            foreach (var visual in visuals)
            {
                if (visual != null)
                {
                    visual.enabled = true;
                }
            }
        }
    }

    public void Hide()
    {
        if (trigger != null)
        {
            trigger.enabled = false;
        }

        if (visuals != null)
        {
            foreach (var visual in visuals)
            {
                if (visual != null)
                {
                    visual.enabled = false;
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (Collect(other.gameObject))
            {
                Hide();
                
                if (respawnTime > 0f)
                {
                    StartCoroutine(RespawnRoutine());
                }
                else
                {
                    Destroy(gameObject);
                }
            }
        }
    }

    protected virtual bool Collect(GameObject collector)
    {
        // Base implementation always succeeds
        // Override in derived classes to add specific collection logic
        // Return true if collection succeeds, false if it fails
        return true;
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnTime);
        Show();
    }
}