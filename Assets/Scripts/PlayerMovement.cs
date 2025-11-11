using UnityEngine;
using System.Collections;

/// <summary>
/// Handles player movement including walking, running, jumping, and speed modifiers
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    #region Movement Configuration
    
    [Header("Speed Settings")]
    [SerializeField] private float baseSpeed = 12f;
    [SerializeField] private float sprintSpeed = 18f;
    
    [Header("Physics Settings")]
    public float gravity = -9.81f * 2;
    public float jumpHeight = 3f;

    [Header("Ground Detection")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;
    
    #endregion

    #region Private Fields
    
    private CharacterController controller;
    private float currentSpeed;
    private Coroutine speedMultiplierRoutine;
    
    private Vector3 velocity;
    private bool isGrounded;
    private bool isMoving;
    private Vector3 lastPosition = Vector3.zero;
    
    #endregion

    #region Unity Lifecycle
    
    void Start()
    {
        InitializeMovement();
    }

    void Update()
    {
        UpdateGroundedState();
        HandleMovementInput();
        HandleJumpInput();
        ApplyGravity();
        UpdateMovingState();
    }
    
    #endregion

    #region Initialization
    
    private void InitializeMovement()
    {
        controller = GetComponent<CharacterController>();
        currentSpeed = baseSpeed; // Initialize current speed
    }
    
    #endregion

    #region Ground Detection
    
    private void UpdateGroundedState()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
    }
    
    #endregion

    #region Movement Input Handling
    
    private void HandleMovementInput()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;

        // Hold Left Shift to sprint while grounded
        float appliedSpeed = ShouldSprint(x, z) ? sprintSpeed : currentSpeed;
        
        controller.Move(move * appliedSpeed * Time.deltaTime);
    }
    
    private bool ShouldSprint(float x, float z)
    {
        return Input.GetKey(KeyCode.LeftShift) && 
               isGrounded && 
               (x != 0f || z != 0f);
    }
    
    private void HandleJumpInput()
    {
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }
    
    #endregion

    #region Physics
    
    private void ApplyGravity()
    {
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
    
    #endregion

    #region Movement State Tracking
    
    private void UpdateMovingState()
    {
        if (lastPosition != transform.position && isGrounded)
        {
            isMoving = true;
        }
        else
        {
            isMoving = false;
        }

        lastPosition = transform.position;
    }
    
    #endregion

    #region Speed Modifiers
    
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
    
    #endregion

    #region Public Properties
    
    public bool IsMoving => isMoving;
    public bool IsGrounded => isGrounded;
    public float CurrentSpeed => currentSpeed;
    
    #endregion
}