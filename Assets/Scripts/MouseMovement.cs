using UnityEngine;

public class MouseMovement : MonoBehaviour
{
    public float mouseSensitivity = 450f;
    public Transform cameraTransform;   // Main Camera (child of Player)
    public Transform weaponHolder;      // drag WeaponHolder (child of Camera)

    float pitch;
    float yawInput;
    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (!cameraTransform)
            cameraTransform = GetComponentInChildren<Camera>()?.transform;
        if (!weaponHolder && cameraTransform)
            weaponHolder = cameraTransform.Find("WeaponHolder");
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity * Time.deltaTime;

        yawInput += mouseX;
        pitch = Mathf.Clamp(pitch - mouseY, -90f, 90f);
    }

    void LateUpdate()
    {
        // Pitch on camera + holder (same pivot as camera)
        if (cameraTransform) cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        if (weaponHolder)    weaponHolder.localRotation    = Quaternion.Euler(pitch, 0f, 0f);
        // (No roll applied)
    }

    void FixedUpdate()
    {
        if (rb) { rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, yawInput, 0f)); }
        else if (Mathf.Abs(yawInput) > Mathf.Epsilon) { transform.Rotate(0f, yawInput, 0f); }
        yawInput = 0f;
    }
}
