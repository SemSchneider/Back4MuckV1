using UnityEngine;

public class Gun : MonoBehaviour
{
    [Header("Gun settings")]
    public int ammo = 30;                // start ammo
    public float fireRate = 0.1f;        // tijd tussen schoten
    public Transform firePoint;          // object bij loop waar de raycast start
    public float range = 100f;           // afstand van het schot

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
        Debug.Log("Trying to shoot! Ammo left: " + ammo); // <-- Hier zet je de debug

        Vector3 origin = firePoint.position;
        Vector3 direction = firePoint.forward;

        Debug.DrawRay(origin, direction * 50f, Color.red, 2f);

        if (Physics.Raycast(origin, direction, out RaycastHit hit, 100f))
            Debug.Log("Hit: " + hit.collider.name);
    }

}
