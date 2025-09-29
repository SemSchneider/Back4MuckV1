using UnityEngine;

public class AKSpin : MonoBehaviour
{
    public float rotationSpeed = 50f;
    public ParticleSystem particles;

    void Update()
    {
        // Draai rond de Y-as
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);
    }

    // Functie om particles te stoppen
    public void StopParticles()
    {
        if (particles != null && particles.isPlaying)
            particles.Stop();
    }
}
