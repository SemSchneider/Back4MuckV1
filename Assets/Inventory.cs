using UnityEngine;

public class Inventory : MonoBehaviour
{
    public Transform weaponHolder; // WeaponHolder child van camera
    private GameObject currentWeapon;

    public void AddWeapon(GameObject weaponPrefab, int ammo)
    {
        EquipWeapon(weaponPrefab, ammo);
    }

    private void EquipWeapon(GameObject prefab, int ammo)
    {
        // verwijder oud wapen
        foreach (Transform child in weaponHolder)
            Destroy(child.gameObject);

        // spawn nieuwe
        currentWeapon = Instantiate(prefab, weaponHolder);
        currentWeapon.transform.localPosition = Vector3.zero;
        currentWeapon.transform.localRotation = Quaternion.identity;

        // ammo doorgeven
        Gun gun = currentWeapon.GetComponent<Gun>();
        if (gun != null) gun.ammo = ammo;
    }
}
