using System.Collections;
using UnityEngine;

public class NewMonoBehaviourScript : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
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

    public enum ShootingMode { Automatic, Burst, Single }
    public ShootingMode currentShootingMode = ShootingMode.Automatic;

    private bool isTriggerHeld;
    private bool isFiring;     // prevents multiple coroutines
    private Coroutine fireLoop;

    private void Awake()
    {
        if (!playerCamera) playerCamera = Camera.main;
        if (!playerCamera) Debug.LogError("Assign playerCamera (or tag a camera MainCamera).", this);
        if (!bulletSpawn) Debug.LogError("Assign bulletSpawn.", this);
        if (!bulletPrefab) Debug.LogError("Assign bulletPrefab (with Rigidbody).", this);
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

    private Vector3 CalculateDirectionWithSpread()
    {
        if (!playerCamera) return transform.forward;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 targetPoint = Physics.Raycast(ray, out RaycastHit hit)
            ? hit.point
            : ray.GetPoint(75f);

        Vector3 baseDir = (targetPoint - bulletSpawn.position).normalized;

        float x = Random.Range(-spreadIntensity, spreadIntensity);
        float y = Random.Range(-spreadIntensity, spreadIntensity);
        Vector3 spread = playerCamera.transform.right * x + playerCamera.transform.up * y;

        return (baseDir + spread).normalized;
    }

    private IEnumerator DestroyBulletAfterTime(GameObject bullet, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (bullet) Destroy(bullet);
    }
}
