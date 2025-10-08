using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; set;}

    public AudioSource shootingSound1911;
    public AudioSource reloadSound1911;
    public AudioSource emptyMagazineSound;

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
