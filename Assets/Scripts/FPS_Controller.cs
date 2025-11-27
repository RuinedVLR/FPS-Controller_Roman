using UnityEngine;

public class FPS_Controller : MonoBehaviour
{
    [Header("Movement")]  
    [SerializeField] private float walkSpeed;
    [SerializeField] private float sprintSpeed;
    [SerializeField] private float crouchSpeed;
    private float speed; // current speed (changes from different states)

    [Header("Jumping")]
    [SerializeField] private float jumpHeight;
    [SerializeField] private float airControl = 2.0f; // 0 = no control, higher = more control in air
    private float gravity = -9.81f;
    private float gravityMult = 3f; // gravity acceleration multiplier

    // Camera movement Axis
    [Header("Camera")]
    [SerializeField] private float maxLook; // Max camera movement (up)
    [SerializeField] private float minLook; // Min camera movement (down)
    [SerializeField] private float sensitivity;
    private float mouseY;
    private float mouseX;

    [Header("Crouching")]
    [SerializeField] private float crouchingTime;
    [SerializeField] private float crouchHeight;
    [SerializeField] private float normalHeight;

    [Header("Keybinds")]
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode crouchKey = KeyCode.LeftControl;

    [Header("Stand check")]
    [SerializeField] private LayerMask standCheckLayers = ~0; // all layers checking by default
    [SerializeField] private float standCheckPadding = 0.02f; // small padding to avoid ground collision

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private CharacterController controller;
    private Vector3 velocity;

    [Header("Checkers")]
    private bool crouching = false;
    private bool isGrounded = true;

    private float cameraOriginalLocalY;

    private Vector3 momentum = Vector3.zero;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.visible = false;

        if (controller != null)
        {
            controller.height = normalHeight;
            controller.center = new Vector3(0, controller.height / 2f, 0);

            // slope limit increase
            controller.slopeLimit = 50f;
            // step offset for smoother walking
            controller.stepOffset = Mathf.Max(controller.stepOffset, 0.3f);
        }

        if (playerCamera != null)
        {
            cameraOriginalLocalY = 1.7f;
        }
    }

    // Update is called once per frame
    void Update()
    {
        isGrounded = controller.isGrounded;

        if (isGrounded && velocity.y == 0)
        {
            velocity.y = 0;
        }

        Crouch();
        Jump();
        Sprint();
        ApplyGravity();
        ApplyMovement();
        CameraMovement();
        
    }

    private void ApplyMovement()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        // get input direction
        Vector3 inputDir = transform.right * moveX + transform.forward * moveZ;
        float inputMag = inputDir.magnitude;
        if (inputMag > 1f) inputDir /= inputMag; // clamp diagonal movement

        float currentSpeed = speed;
        Vector3 desiredHorizontal = inputDir * currentSpeed; // desired horizontal velocity

        if (controller.isGrounded)
        {
            momentum = desiredHorizontal;
        }
        else
        {
            // airborne — smoothly lerp momentum towards input (air control)
            if (inputDir.sqrMagnitude > 0.001f)
            {
                momentum = Vector3.Lerp(momentum, desiredHorizontal, airControl * Time.deltaTime);
            }
        }

        // combine horizontal momentum with vertical velocity
        Vector3 move = momentum + new Vector3(0, velocity.y, 0);
        controller.Move(move * Time.deltaTime); // * deltaTime for frame rate independence
    }

    private void CameraMovement()
    {
        mouseX = Input.GetAxis("Mouse X") * sensitivity;
        mouseY -= Input.GetAxis("Mouse Y") * -sensitivity;

        mouseY = Mathf.Clamp(mouseY, minLook, maxLook);

        playerCamera.transform.localRotation = Quaternion.Euler(-mouseY, 0, 0);
        transform.rotation *= Quaternion.Euler(0, mouseX, 0);
    }

    private void ApplyGravity()
    {
        if (controller.isGrounded)
        {
            if (velocity.y <= 0f)
                velocity.y = -1.0f;
        }
        else
        {
            velocity.y += gravity * gravityMult * Time.deltaTime;
        }
    }

    private void Crouch()
    {
        if (Input.GetKeyDown(crouchKey))
        {
            // if crouching - try to stand up
            if (crouching)
            {
                if (CanStandUp())
                {
                    crouching = false;
                }
                else
                {
                    // if cant stand up - stay crouched
                    crouching = true;
                }
            }
            else
            {
                // if standing - crouch
                crouching = true;
            }
        }

        if (crouching)
        {
            speed = crouchSpeed;
        }
        else
        {
            speed = walkSpeed;
        }

        float previousHeight = controller.height;
        float targetHeight = crouching ? crouchHeight : normalHeight;

        float minAllowed = controller.radius * 2f + 0.01f;
        targetHeight = Mathf.Max(targetHeight, minAllowed);

        float newHeight = Mathf.MoveTowards(previousHeight, targetHeight, crouchingTime * Time.deltaTime);

        float heightDelta = previousHeight - newHeight;
        if (heightDelta > 0.0001f)
        {
            controller.Move(Vector3.up * (heightDelta / 2f));
        }

        controller.height = newHeight;
        controller.center = new Vector3(0, controller.height / 2f, 0);

        if (playerCamera != null)
        {
            float cameraShift = normalHeight - crouchHeight;
            float targetCameraY = crouching ? cameraOriginalLocalY - cameraShift : cameraOriginalLocalY;
            Vector3 camLocalPos = playerCamera.transform.localPosition;
            camLocalPos.y = Mathf.MoveTowards(camLocalPos.y, targetCameraY, crouchingTime * Time.deltaTime);
            playerCamera.transform.localPosition = camLocalPos;
        }
    }

    private bool CanStandUp()
    {
        if (controller == null)
            return true;

        // world-space bottom point of current controller
        Vector3 worldCenter = transform.position + controller.center;
        float currentHalfHeight = controller.height * 0.5f;
        Vector3 bottom = worldCenter - Vector3.up * currentHalfHeight;

        // desired top if standing
        Vector3 desiredTop = bottom + Vector3.up * normalHeight;

        // add sensible padding using skinWidth to avoid touching ground/self
        float padding = controller.skinWidth + standCheckPadding;
        Vector3 checkStart = bottom + Vector3.up * padding;
        Vector3 checkEnd = desiredTop - Vector3.up * padding;

        float checkRadius = Mathf.Max(0.01f, controller.radius - 0.01f);

        // use OverlapCapsule so we can filter out self and children
        Collider[] hits = Physics.OverlapCapsule(checkStart, checkEnd, checkRadius, standCheckLayers, QueryTriggerInteraction.Ignore);
        foreach (var col in hits)
        {
            if (col == null) continue;
            // ignore self and child colliders
            if (col.transform == transform || col.transform.IsChildOf(transform)) continue;
            // found obstacle above — cannot stand
            return false;
        }

        return true;
    }

    private void Jump()
    {
        if (Input.GetKeyDown(jumpKey) && controller.isGrounded && CanStandUp())
        {
            // If crouching, stand up first
            if (crouching)
            {
                crouching = false;
                // center and height reset
                controller.height = normalHeight;
                controller.center = new Vector3(0, controller.height / 2f, 0);
                controller.Move(Vector3.up * 0.05f);
            }

            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity * gravityMult);
        }
    }

    private void Sprint()
    {
        // if crouching, cannot sprint
        if (crouching)
        {
            speed = crouchSpeed;
            return;
        }

        if (Input.GetKey(sprintKey))
        {
            speed = sprintSpeed;
        }
        else
        {
            speed = walkSpeed;
        }
    }
}
