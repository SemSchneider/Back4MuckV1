using System.Collections.Generic;
using UnityEngine;

public class BeerBottle : MonoBehaviour
{
    public List<Rigidbody> beerPieces = new List<Rigidbody>();

    public void Shatter()
    {
        foreach (Rigidbody piece in beerPieces)
        {
            piece.isKinematic = false;
            piece.AddExplosionForce(100f, transform.position, 5f);
        }
    }
}
