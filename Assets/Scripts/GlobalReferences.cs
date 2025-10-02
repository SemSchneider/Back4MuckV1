using UnityEngine;

public class GlobalReferences : MonoBehaviour
{
    public static GlobalReferences Instance { get; set;}

    public GameObject player;
    public GameObject playerCamera;
    public GameObject weapon;
    public GameObject bulletImpactEffectPrefab;

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
}
