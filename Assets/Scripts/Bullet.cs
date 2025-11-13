using UnityEngine;

public class Bullet : MonoBehaviour
{
    // Uncomment the line below to enable bullet hit debugging
    // #define DEBUG_BULLET_HITS
    
    private void OnCollisionEnter(Collision objectWeHit)
    {
        if (objectWeHit.gameObject.CompareTag("Target"))
        {
#if DEBUG_BULLET_HITS
            print("hit " + objectWeHit.gameObject.name);
#endif
            CreateBulletImpactEffect(objectWeHit);
            Destroy(gameObject);
        }

        if (objectWeHit.gameObject.CompareTag("Wall"))
        {
#if DEBUG_BULLET_HITS
            print("hit a wall");
#endif
            CreateBulletImpactEffect(objectWeHit);
            Destroy(gameObject);
        }
        
        if (objectWeHit.gameObject.CompareTag("Beer"))
        {
#if DEBUG_BULLET_HITS
            print("hit a beer");
#endif
            objectWeHit.gameObject.GetComponent<BeerBottle>().Shatter();
        }
        
        // Check if we hit an enemy
        SimpleEnemy simpleEnemy = objectWeHit.gameObject.GetComponent<SimpleEnemy>();
        if (simpleEnemy != null)
        {
#if DEBUG_BULLET_HITS
            print("hit SimpleEnemy: " + objectWeHit.gameObject.name);
#endif
            simpleEnemy.TakeDamage(25f); // Deal 25 damage to enemy
            CreateBulletImpactEffect(objectWeHit);
            Destroy(gameObject);
            return;
        }
        
        // Check if we hit a tank enemy
        TankEnemy tankEnemy = objectWeHit.gameObject.GetComponent<TankEnemy>();
        if (tankEnemy != null)
        {
#if DEBUG_BULLET_HITS
            print("hit TankEnemy: " + objectWeHit.gameObject.name);
#endif
            tankEnemy.TakeDamage(25f); // Deal 25 damage to tank enemy
            CreateBulletImpactEffect(objectWeHit);
            Destroy(gameObject);
            return;
        }
        
        // Check if we hit a fast enemy
        FastEnemy fastEnemy = objectWeHit.gameObject.GetComponent<FastEnemy>();
        if (fastEnemy != null)
        {
#if DEBUG_BULLET_HITS
            print("hit FastEnemy: " + objectWeHit.gameObject.name);
#endif
            fastEnemy.TakeDamage(25f); // Deal 25 damage to fast enemy
            CreateBulletImpactEffect(objectWeHit);
            Destroy(gameObject);
            return;
        }

    }
    void CreateBulletImpactEffect(Collision objectWeHit)
    {
        if (objectWeHit == null || objectWeHit.contactCount == 0)
        {
            Debug.LogWarning("No collision contacts available for bullet impact.", this);
            return;
        }

        var globalRefs = GlobalReferences.Instance;
        if (globalRefs == null)
        {
            Debug.LogError("GlobalReferences.Instance is null. Ensure a GlobalReferences object exists in the scene.", this);
            return;
        }

        var impactPrefab = globalRefs.bulletImpactEffectPrefab;
        if (impactPrefab == null)
        {
            Debug.LogError("bulletImpactEffectPrefab is not assigned on GlobalReferences.", globalRefs);
            return;
        }

        ContactPoint contact = objectWeHit.contacts[0];

        GameObject hole = Instantiate(
            impactPrefab,
            contact.point + contact.normal * 0.001f,   // slight offset to avoid z-fighting
            Quaternion.LookRotation(contact.normal)      // rotate to align with surface normal
        );
        hole.transform.SetParent(objectWeHit.gameObject.transform);
    }
}

