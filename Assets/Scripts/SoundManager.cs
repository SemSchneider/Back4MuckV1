using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; set;}

    public AudioSource shootingSound1911;
    public AudioSource reloadSound1911;
    public AudioSource emptyMagazineSound;

		// AK74 specific sounds
		public AudioSource shootingSoundAK74;
		public AudioSource reloadSoundAK74;
		
		// Zombie feedback sounds
		public AudioSource zombieHitSound;
		public AudioSource zombieDeathSound;

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
    
    /// <summary>
    /// Play zombie hit sound at the specified world position
    /// </summary>
    /// <param name="position">World position where the sound should play</param>
    public void PlayZombieHitSound(Vector3 position)
    {
        if (zombieHitSound == null || zombieHitSound.clip == null)
        {
            Debug.LogWarning("SoundManager: No zombie hit sound configured");
            return;
        }
        
        // Play sound at specified position
        if (zombieHitSound.isActiveAndEnabled && zombieHitSound.gameObject.activeInHierarchy)
        {
            zombieHitSound.transform.position = position;
            zombieHitSound.PlayOneShot(zombieHitSound.clip);
        }
        else
        {
            // Fallback: create a one-shot at the position
            AudioSource.PlayClipAtPoint(zombieHitSound.clip, position, zombieHitSound.volume);
        }
    }
    
    /// <summary>
    /// Play zombie death sound at the specified world position
    /// </summary>
    /// <param name="position">World position where the sound should play</param>
    public void PlayZombieDeathSound(Vector3 position)
    {
        if (zombieDeathSound == null || zombieDeathSound.clip == null)
        {
            Debug.LogWarning("SoundManager: No zombie death sound configured");
            return;
        }
        
        // Play sound at specified position
        if (zombieDeathSound.isActiveAndEnabled && zombieDeathSound.gameObject.activeInHierarchy)
        {
            zombieDeathSound.transform.position = position;
            zombieDeathSound.PlayOneShot(zombieDeathSound.clip);
        }
        else
        {
            // Fallback: create a one-shot at the position
            AudioSource.PlayClipAtPoint(zombieDeathSound.clip, position, zombieDeathSound.volume);
        }
    }
}
