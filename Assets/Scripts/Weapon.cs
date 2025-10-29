using System.Collections;
using UnityEngine;
using TMPro;

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
    [Tooltip("Layers considered for aiming raycast (e.g., exclude Player/Weapon)")]
    public LayerMask aimMask = ~0; // default: Everything
    [Tooltip("Max distance to aim toward when nothing is hit by raycast")]
    public float maxAimDistance = 100f;

    public GameObject muzzleFlashEffectPrefab;
    public Animator weaponAnimator;
    public string shootTriggerName = "RECOIL";
    [Tooltip("Animator trigger name for reload animation (optional)")]
    public string reloadTriggerName = "RELOAD";

    [Header("Ammo / Reload")]
    public float reloadTime = 1.6f;
    [Min(1)]
    public int magazineSize = 12;
    public int bulletLeft;
    public bool isReloading;

    [Header("UI")]
    public TMP_Text ammoText;

    [Header("Weapon Positioning")]
    [Tooltip("Proper position when equipped in player's hands")]
    public Vector3 equippedPosition = Vector3.zero;
    [Tooltip("Proper rotation when equipped in player's hands")]
    public Vector3 equippedRotation = Vector3.zero;
    [Tooltip("Proper scale when equipped in player's hands")]
    public Vector3 equippedScale = Vector3.one;

    public enum WeaponAudioProfile { Colt1911, AK74 }
    [Header("Audio Profile")]
    public WeaponAudioProfile audioProfile = WeaponAudioProfile.Colt1911;

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

        bulletLeft = magazineSize;
        UpdateAmmoUI();
    }

    private void OnEnable()
    {
        UpdateAmmoUI();
    }

    /// <summary>
    /// Positions the weapon correctly when equipped in player's hands
    /// </summary>
    public void SetEquippedPosition()
    {
        if (transform.parent != null)
        {
            transform.localPosition = equippedPosition;
            transform.localRotation = Quaternion.Euler(equippedRotation);
            transform.localScale = equippedScale;
        }
    }

    /// <summary>
    /// Captures the current transform values as the equipped position (useful for setup)
    /// </summary>
    [ContextMenu("Capture Current Position as Equipped")]
    public void CaptureCurrentPositionAsEquipped()
    {
        if (transform.parent != null)
        {
            equippedPosition = transform.localPosition;
            equippedRotation = transform.localEulerAngles;
            equippedScale = transform.localScale;
            Debug.Log($"Captured equipped position: {equippedPosition}, rotation: {equippedRotation}, scale: {equippedScale}", this);
        }
        else
        {
            Debug.LogWarning("Weapon must be a child of another object to capture equipped position.", this);
        }
    }

    private void OnValidate()
    {
        // Keep values sane when editing in the Inspector
        magazineSize = Mathf.Max(1, magazineSize);
        if (!Application.isPlaying)
        {
            // Make sure starting ammo does not exceed mag size in edit mode
            bulletLeft = Mathf.Clamp(bulletLeft, 0, magazineSize);
            UpdateAmmoUI();
        }
    }

    [ContextMenu("Refill Magazine")]
    private void RefillMagazine()
    {
        bulletLeft = magazineSize;
        UpdateAmmoUI();
    }

    private void Update()
    {
        // Manual reload input
        if (Input.GetKeyDown(KeyCode.R))
        {
            TryStartReload();
        }

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

        // Start the fire loop when trigger engages; block if empty to avoid an extra shot edge case
        if (isTriggerHeld && !isFiring)
        {
            if (isReloading)
            {
                return;
            }
            if (bulletLeft <= 0)
            {
                PlayEmptyMagSoundIfAvailable();
                TryStartReload();
                isTriggerHeld = false;
                return;
            }
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
                if (isReloading) { yield return null; continue; }
                if (bulletLeft <= 0)
                {
                    // auto-reload on empty when attempting to fire
                    PlayEmptyMagSoundIfAvailable();
                    TryStartReload();
                    yield return null;
                    continue;
                }
                int shotsRemainingInBurst = bulletsPerBurst;
                while (shotsRemainingInBurst-- > 0)
                {
                    if (isReloading || bulletLeft <= 0) break;
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
            if (isReloading)
            {
                isFiring = false;
                yield break;
            }
            if (bulletLeft <= 0)
            {
                PlayEmptyMagSoundIfAvailable();
                TryStartReload();
                isFiring = false;
                yield break;
            }
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
            if (isReloading)
            {
                yield return null;
                continue;
            }
            if (bulletLeft <= 0)
            {
                PlayEmptyMagSoundIfAvailable();
                TryStartReload();
                yield return null;
                continue;
            }
            FireOnce();
            yield return new WaitForSeconds(shootingDelay);
        }

        isFiring = false;
    }

    private void FireOnce()
    {
        if (!bulletPrefab || !bulletSpawn) return;
        if (bulletLeft <= 0 || isReloading) return;

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
            // Use direct velocity for consistent projectile speed regardless of mass/impulse
            rb.linearVelocity = dir * bulletVelocity;
        }

        if (bulletPrefabLifeTime > 0f) StartCoroutine(DestroyBulletAfterTime(bullet, bulletPrefabLifeTime));
        bulletLeft = Mathf.Max(0, bulletLeft - 1);
        UpdateAmmoUI();
        // Debug to confirm it fired:
        // Debug.Log("FIRE", this);
    }

    private void TryStartReload()
    {
        if (isReloading) return;
        if (bulletLeft >= magazineSize) return;
        StartCoroutine(ReloadRoutine());
    }

    private IEnumerator ReloadRoutine()
    {
        isReloading = true;
        PlayReloadSoundIfAvailable();
        if (weaponAnimator && !string.IsNullOrEmpty(reloadTriggerName))
        {
            weaponAnimator.ResetTrigger(reloadTriggerName);
            weaponAnimator.SetTrigger(reloadTriggerName);
        }
        if (reloadTime > 0f)
        {
            yield return new WaitForSeconds(reloadTime);
        }
        bulletLeft = magazineSize;
        isReloading = false;
        UpdateAmmoUI();
    }

    private void UpdateAmmoUI()
    {
        if (AmmoManagement.Instance)
        {
            AmmoManagement.Instance.UpdateAmmo(bulletLeft, magazineSize);
        }
        else if (ammoText)
        {
            ammoText.text = bulletLeft.ToString() + "/" + magazineSize.ToString();
        }
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
        AudioSource src = null;
        switch (audioProfile)
        {
            case WeaponAudioProfile.AK74:
                src = SoundManager.Instance.shootingSoundAK74 ? SoundManager.Instance.shootingSoundAK74 : SoundManager.Instance.shootingSound1911;
                break;
            case WeaponAudioProfile.Colt1911:
            default:
                src = SoundManager.Instance.shootingSound1911;
                break;
        }
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

    private float lastEmptyMagPlayTime;
    private const float emptyMagCooldown = 0.2f; // avoid spamming the click sound every frame

    private void PlayEmptyMagSoundIfAvailable()
    {
        if (SoundManager.Instance == null) return;
        var src = SoundManager.Instance.emptyMagazineSound;
        if (!src) return;

        // rate-limit
        if (Time.time - lastEmptyMagPlayTime < emptyMagCooldown) return;
        lastEmptyMagPlayTime = Time.time;

        var clip = src.clip;
        if (!clip) return;

        Vector3 playPosition = bulletSpawn ? bulletSpawn.position : transform.position;
        if (src.isActiveAndEnabled && src.gameObject.activeInHierarchy)
        {
            if (bulletSpawn) src.transform.position = playPosition;
            src.PlayOneShot(clip);
        }
        else
        {
            AudioSource.PlayClipAtPoint(clip, playPosition, Mathf.Clamp01(src.volume));
        }
    }

    private void PlayReloadSoundIfAvailable()
    {
        if (SoundManager.Instance == null) return;
        AudioSource src = null;
        switch (audioProfile)
        {
            case WeaponAudioProfile.AK74:
                src = SoundManager.Instance.reloadSoundAK74 ? SoundManager.Instance.reloadSoundAK74 : SoundManager.Instance.reloadSound1911;
                break;
            case WeaponAudioProfile.Colt1911:
            default:
                src = SoundManager.Instance.reloadSound1911;
                break;
        }
        if (!src) return;

        var clip = src.clip;
        if (!clip) return;

        Vector3 playPosition = bulletSpawn ? bulletSpawn.position : transform.position;
        if (src.isActiveAndEnabled && src.gameObject.activeInHierarchy)
        {
            if (bulletSpawn) src.transform.position = playPosition;
            src.PlayOneShot(clip);
        }
        else
        {
            AudioSource.PlayClipAtPoint(clip, playPosition, Mathf.Clamp01(src.volume));
        }
    }

    private Vector3 CalculateDirectionWithSpread()
    {
        if (!mainCamera) return transform.forward;

        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 targetPoint = Physics.Raycast(ray, out RaycastHit hit, maxAimDistance, aimMask, QueryTriggerInteraction.Ignore)
            ? hit.point
            : ray.GetPoint(maxAimDistance);

        Vector3 baseDir = (targetPoint - bulletSpawn.position).normalized;
        if (spreadIntensity <= 0f)
        {
            return baseDir;
        }

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
