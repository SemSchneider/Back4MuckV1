using NUnit.Framework;
using UnityEngine;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    private CharacterController controller;
    
    [Header("Speed Settings")]
    [SerializeField] private float baseSpeed = 12f;
    [SerializeField] private float sprintSpeed = 18f;
    private float currentSpeed;
    private Coroutine speedMultiplierRoutine;
    public float gravity = -9.81f * 2;
    public float jumpHeight = 3f;

    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    Vector3 velocity;
    bool isGrounded;
    bool isMoving;

    private Vector3 lastPosition = new Vector3(0f,0f,0f);
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        controller = GetComponent<CharacterController>();
        currentSpeed = baseSpeed; // Initialize current speed
    }

    public void ApplySpeedMultiplier(float multiplier, float duration)
    {
        if (duration <= 0f)
        {
            Debug.LogWarning("Trying to apply speed multiplier with duration <= 0", this);
            return;
        }

        // If there's already a speed multiplier active, stop it
        if (speedMultiplierRoutine != null)
        {
            StopCoroutine(speedMultiplierRoutine);
        }

        // Apply new multiplier
        currentSpeed = baseSpeed * multiplier;
        
        // Start new timer
        speedMultiplierRoutine = StartCoroutine(ResetSpeedAfterDelay(duration));
    }

    private IEnumerator ResetSpeedAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        currentSpeed = baseSpeed;
        speedMultiplierRoutine = null;
    }

    // Update is called once per frame
    void Update()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

		Vector3 move = transform.right * x + transform.forward * z;

		// hold Left Shift to sprint while grounded
		float appliedSpeed = (Input.GetKey(KeyCode.LeftShift) && isGrounded && (x != 0f || z != 0f)) ? sprintSpeed : currentSpeed;
		controller.Move(move * appliedSpeed * Time.deltaTime);

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        if (lastPosition != gameObject.transform.position && isGrounded == true)
        {
            isMoving = true;
            // lastPosition = transform.position;
        }
        else
        {
            isMoving = false;
        }

        lastPosition = transform.position;
    }
}
