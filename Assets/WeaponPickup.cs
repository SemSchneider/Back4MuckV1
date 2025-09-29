using UnityEngine;
using TMPro; // als je TextMeshPro gebruikt

public class WeaponPickup : MonoBehaviour
{
    public GameObject weaponPrefab;
    public int ammoAmount = 30;
    public TextMeshProUGUI pickupText; // sleep hier je UI tekst element in

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Inventory inv = other.GetComponent<Inventory>();
            if (inv != null)
            {
                inv.AddWeapon(weaponPrefab, ammoAmount);

                // UI tekst tonen
                if (pickupText != null)
                {
                    pickupText.text = "Acquired AK47";
                    StartCoroutine(ClearText());
                }

                // Stop particles voordat we het object vernietigen
                AKSpin spin = GetComponentInChildren<AKSpin>();
                if (spin != null)
                    spin.StopParticles();

                Destroy(gameObject);
            }
        }
    }


    private System.Collections.IEnumerator ClearText()
    {
        yield return new WaitForSeconds(2f); // tekst 2 seconden zichtbaar
        pickupText.text = "";
    }


}
