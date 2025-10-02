using UnityEngine;

public class Bullet : MonoBehaviour
{
    private void OnCollisionEnter(Collision objectWeHit)
    {
        if (objectWeHit.gameObject.CompareTag("Target"))
        {
            print("hit " + objectWeHit.gameObject.name);

            CreateBulletImpactEffect(objectWeHit);

            Destroy(gameObject);
        }

        if (objectWeHit.gameObject.CompareTag("Wall"))
        {
            print("hit a wall");

            CreateBulletImpactEffect(objectWeHit);


            Destroy(gameObject);
        }

    }
    void CreateBulletImpactEffect(Collision objectWeHit)
    {
        ContactPoint contact = objectWeHit.contacts[0];

        GameObject hole = Instantiate(
            GlobalReferences.Instance.bulletImpactEffectPrefab,
            contact.point + contact.normal * 0.001f,   // slight offset to avoid z-fighting
            Quaternion.LookRotation(contact.normal)      // rotate to align with surface normal
        );
        hole.transform.SetParent(objectWeHit.gameObject.transform);
    }
}

