using UnityEngine;

public class FlashlightToggle : MonoBehaviour
{
    public Light flashlight; // Sleep hier je Spot Light in
    public KeyCode toggleKey = KeyCode.F;

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            // Toggle aan/uit
            flashlight.enabled = !flashlight.enabled;
        }
    }
}
