using UnityEngine;

public class Gun : MonoBehaviour
{
    [Header("Gun settings")]
    public int ammo = 30;                // start ammo
    public float fireRate = 0.1f;        // tijd tussen schoten
    public Transform firePoint;          // object bij loop waar de raycast start
    public float range = 100f;           // afstand van het schot
    public float damage = 25f;           // damage per shot

    private float nextFireTime = 0f;

    void Update()
    {
        if (Input.GetButton("Fire1") && Time.time >= nextFireTime && ammo > 0)
        {
            Shoot();
            nextFireTime = Time.time + fireRate;
        }
    }

    void Shoot()
    {
        ammo--;
        Debug.Log("Trying to shoot! Ammo left: " + ammo);

        Vector3 origin = firePoint.position;
        Vector3 direction = firePoint.forward;

        Debug.DrawRay(origin, direction * range, Color.red, 2f);

        if (Physics.Raycast(origin, direction, out RaycastHit hit, range))
        {
            Debug.Log("Hit: " + hit.collider.name);
            
            // Check for different enemy types and deal damage
            var simpleEnemy = hit.collider.GetComponent<SimpleEnemy>();
            if (simpleEnemy != null)
            {
                simpleEnemy.TakeDamage(damage);
                Debug.Log($"Dealt {damage} damage to SimpleEnemy");
                return;
            }
            
            var tankEnemy = hit.collider.GetComponent<TankEnemy>();
            if (tankEnemy != null)
            {
                tankEnemy.TakeDamage(damage);
                Debug.Log($"Dealt {damage} damage to TankEnemy");
                return;
            }
            
            var fastEnemy = hit.collider.GetComponent<FastEnemy>();
            if (fastEnemy != null)
            {
                fastEnemy.TakeDamage(damage);
                Debug.Log($"Dealt {damage} damage to FastEnemy");
                return;
            }
            
            Debug.Log("Hit object does not have an enemy component");
        }
        else
        {
            Debug.Log("Shot missed - no target hit");
        }
    }

}
