using System.Collections;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    private Camera mainCamera;
    [Header("Fire Settings")]
    public float shootingDelay = 0.1f;     // seconds between shots
    public int bulletsPerBurst = 3;
    public float spreadIntensity = 0.02f;
    [Tooltip("Delay between shots inside a burst (Burst mode)")]
    public float burstShotDelay = 0.2f;
    [Tooltip("Delay between bursts while holding trigger (Burst mode)")]
    public float burstInterval = 2f;
    [Tooltip("Lockout time after a single shot (Single mode)")]
    public float singleShotDelay = 0.4f;

    [Header("Bullet")]
    public GameObject bulletPrefab;
    public Transform bulletSpawn;
    public float bulletVelocity = 30f;
    public float bulletPrefabLifeTime = 3f;

    public GameObject muzzleFlashEffectPrefab;
    public Animator weaponAnimator;
    public string shootTriggerName = "RECOIL";

    public enum ShootingMode { Automatic, Burst, Single }
    public ShootingMode currentShootingMode = ShootingMode.Automatic;

    private bool isTriggerHeld;
    private bool isFiring;     // prevents multiple coroutines
    private Coroutine fireLoop;

    private void Awake()
    {
        mainCamera = Camera.main;
        if (!mainCamera) Debug.LogError("No Camera tagged MainCamera was found.", this);
        if (!bulletSpawn) Debug.LogError("Assign bulletSpawn.", this);
        if (!bulletPrefab) Debug.LogError("Assign bulletPrefab (with Rigidbody).", this);

        if (!weaponAnimator)
        {
            weaponAnimator = GetComponentInChildren<Animator>();
        }
    }

    private void Update()
    {
        switch (currentShootingMode)
        {
            case ShootingMode.Automatic:
                isTriggerHeld = Input.GetMouseButton(0);
                break;

            case ShootingMode.Burst:
                if (Input.GetKeyDown(KeyCode.Mouse0)) isTriggerHeld = true;
                if (Input.GetKeyUp(KeyCode.Mouse0)) isTriggerHeld = false;
                break;

            case ShootingMode.Single:
                if (Input.GetKeyDown(KeyCode.Mouse0)) isTriggerHeld = true;
                if (Input.GetKeyUp(KeyCode.Mouse0)) isTriggerHeld = false;
                break;
        }

        // Start the fire loop when trigger engages; the coroutine manages its own lifetime
        if (isTriggerHeld && !isFiring)
        {
            isFiring = true;
            fireLoop = StartCoroutine(FireLoop());
        }
    }

    private IEnumerator FireLoop()
    {
        // isFiring is set before starting this coroutine

        if (currentShootingMode == ShootingMode.Burst)
        {
            // While the trigger remains held, fire bursts with an interval between bursts
            while (isTriggerHeld)
            {
                int shotsRemainingInBurst = bulletsPerBurst;
                while (shotsRemainingInBurst-- > 0)
                {
                    FireOnce();
                    // If player releases during a burst, stop early
                    if (!isTriggerHeld) break;
                    yield return new WaitForSeconds(burstShotDelay);
                }

                // If trigger was released during the burst, exit without waiting the interval
                if (!isTriggerHeld) break;

                // Wait between bursts
                yield return new WaitForSeconds(burstInterval);
            }

            isFiring = false;
            yield break;
        }

        if (currentShootingMode == ShootingMode.Single)
        {
            FireOnce();
            // wait once so holding mouse doesn’t auto-repeat
            yield return new WaitForSeconds(singleShotDelay);
            isTriggerHeld = false;
            isFiring = false;
            yield break;
        }

        // Automatic
        while (isTriggerHeld)
        {
            FireOnce();
            yield return new WaitForSeconds(shootingDelay);
        }

        isFiring = false;
    }

    private void FireOnce()
    {
        if (!bulletPrefab || !bulletSpawn) return;

        TriggerShootAnimation();
        PlayShootSound();
        SpawnMuzzleFlash();

        Vector3 dir = CalculateDirectionWithSpread();
        GameObject bullet = Instantiate(bulletPrefab, bulletSpawn.position, Quaternion.LookRotation(dir));

        var rb = bullet.GetComponent<Rigidbody>();
        if (!rb)
        {
            Debug.LogError("bulletPrefab needs a Rigidbody.", bullet);
        }
        else
        {
            rb.AddForce(dir * bulletVelocity, ForceMode.Impulse);
        }

        if (bulletPrefabLifeTime > 0f) StartCoroutine(DestroyBulletAfterTime(bullet, bulletPrefabLifeTime));
        // Debug to confirm it fired:
        // Debug.Log("FIRE", this);
    }

    private void TriggerShootAnimation()
    {
        if (!weaponAnimator || string.IsNullOrEmpty(shootTriggerName)) return;
        weaponAnimator.ResetTrigger(shootTriggerName);
        weaponAnimator.SetTrigger(shootTriggerName);
    }

    private void PlayShootSound()
    {
        if (SoundManager.Instance == null) return;
        var src = SoundManager.Instance.shootingSound1911;
        if (!src)
        {
            return;
        }

        var clip = src.clip;
        if (!clip)
        {
            Debug.LogWarning("SoundManager.shootingSound1911 has no clip assigned.", SoundManager.Instance);
            return;
        }

        // Prefer playing on the configured AudioSource if it is usable
        if (src.isActiveAndEnabled && src.gameObject.activeInHierarchy)
        {
            // Snap to muzzle for positional audio, if desired
            if (bulletSpawn)
            {
                src.transform.position = bulletSpawn.position;
            }
            src.PlayOneShot(clip);
            return;
        }

        // Fallback: create a temp one-shot at the muzzle so it always plays
        Vector3 playPosition = bulletSpawn ? bulletSpawn.position : transform.position;
        float volume = Mathf.Clamp01(src.volume);
        AudioSource.PlayClipAtPoint(clip, playPosition, volume);

        if (FindObjectOfType<AudioListener>() == null)
        {
            Debug.LogWarning("No AudioListener found in the scene; sounds may be inaudible.", this);
        }
    }

    private void SpawnMuzzleFlash()
    {
        if (!bulletSpawn) return;
        if (!muzzleFlashEffectPrefab)
        {
            Debug.LogWarning("No muzzleFlashEffectPrefab assigned on Weapon.", this);
            return;
        }

        GameObject effect = Instantiate(
            muzzleFlashEffectPrefab,
            bulletSpawn.position,
            bulletSpawn.rotation,
            bulletSpawn
        );

        // Ensure effect is visible/playing even if prefab has Play On Awake disabled
        if (!effect.activeSelf) effect.SetActive(true);

        // Force play all particle systems if present
        var allSystems = effect.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < allSystems.Length; i++)
        {
            allSystems[i].Play(true);
        }

        float lifetime = 0f;

        var primaryPs = effect.GetComponent<ParticleSystem>();
        if (primaryPs)
        {
            var main = primaryPs.main;
            lifetime = Mathf.Max(lifetime, main.duration + main.startLifetime.constantMax);
        }
        else
        {
            var systems = effect.GetComponentsInChildren<ParticleSystem>();
            if (systems != null && systems.Length > 0)
            {
                float maxDuration = 0f;
                for (int i = 0; i < systems.Length; i++)
                {
                    var m = systems[i].main;
                    float d = m.duration + m.startLifetime.constantMax;
                    if (d > maxDuration) maxDuration = d;
                }
                lifetime = Mathf.Max(lifetime, maxDuration);
            }
        }

        if (lifetime <= 0f) lifetime = 2f; // safe fallback

        Destroy(effect, lifetime);
    }

    private Vector3 CalculateDirectionWithSpread()
    {
        if (!mainCamera) return transform.forward;

        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 targetPoint = Physics.Raycast(ray, out RaycastHit hit)
            ? hit.point
            : ray.GetPoint(75f);

        Vector3 baseDir = (targetPoint - bulletSpawn.position).normalized;

        float x = Random.Range(-spreadIntensity, spreadIntensity);
        float y = Random.Range(-spreadIntensity, spreadIntensity);
        Vector3 spread = mainCamera.transform.right * x + mainCamera.transform.up * y;

        return (baseDir + spread).normalized;
    }

    private IEnumerator DestroyBulletAfterTime(GameObject bullet, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (bullet) Destroy(bullet);
    }
}
