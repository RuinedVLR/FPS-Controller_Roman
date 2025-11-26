using System.Linq.Expressions;
using UnityEngine;

public class FPS_Controller : MonoBehaviour
{
    [Header("Movement")]  
    [SerializeField] private float walkSpeed;
    [SerializeField] private float sprintSpeed;
    [SerializeField] private float crouchSpeed;
    private float speed;

    [Header("Jumping")]
    [SerializeField] private float jumpHeight;
    private float gravity = -9.81f;
    private float gravityMult = 3f;

    // Camera movement Axis
    [Header("Camera")]
    [SerializeField] private float maxLook; // Max camera movement (up)
    [SerializeField] private float minLook; // Min camera movement (down)
    [SerializeField] private float sensitivity;
    private float mouseY;
    private float mouseX;

    [Header("Crouching")]
    private float crouchingTime = 2f;
    private float crouchHeight = 0.7f;
    private float normalHeight = 2f;

    [Header("Keybinds")]
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode crouchKey = KeyCode.LeftControl;

    private Vector3 offset = new Vector3(0, 1.5f, 0);

    [SerializeField] private Transform player;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private CharacterController controller;
    private Vector3 velocity;

    [Header("Checkers")]
    private bool crouching = false;
    private bool isGrounded = true;

    public MovementState state;
    public enum MovementState
    {
        walking,
        sprinting,
        airborn
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.visible = false;
    }

    // Update is called once per frame
    void Update()
    {
        isGrounded = controller.isGrounded;

        if (isGrounded && velocity.y == 0)
        {
            velocity.y = 0;
        }

        #region Player Movement
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");
        float moveY = velocity.y;
        Vector3 move = transform.right * moveX + transform.forward * moveZ + transform.up * moveY;

        if (!crouching)
        {
            controller.Move(move * walkSpeed * Time.deltaTime);
        }

        if (crouching)
        {
            controller.Move(move * crouchSpeed * Time.deltaTime);
        }
        #endregion

        #region Camera Movement
        mouseX = Input.GetAxis("Mouse X") * sensitivity;
        mouseY -= Input.GetAxis("Mouse Y") * -sensitivity;

        mouseY = Mathf.Clamp(mouseY, minLook, maxLook);

        playerCamera.transform.localRotation = Quaternion.Euler(-mouseY, 0, 0);
        transform.rotation *= Quaternion.Euler(0, mouseX, 0);
        #endregion

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        ApplyGravity();

        #region Crouching
        

        if (Input.GetKeyDown(crouchKey))
        {
            crouching = !crouching;
        }

        if (crouching == true)
        {
            Debug.Log("Crouched");
            controller.height = controller.height - crouchingTime * Time.deltaTime;
            if (controller.height <= crouchHeight)
            {
                controller.height = crouchHeight;
            }
        }

        if (crouching == false)
        {
            Debug.Log("Uncrouched");
            controller.height = controller.height + crouchingTime * Time.deltaTime;

            if (controller.height < normalHeight)
            {
                player.gameObject.SetActive(false);
                player.position = player.position + offset * Time.deltaTime;
                player.gameObject.SetActive(true);
            }

            if (controller.height >= normalHeight)
            {
                controller.height = normalHeight;
            }
        }
        #endregion
    }

    private void ApplyGravity()
    {
        //Debug.Log("Applying gravity");
        if (controller.isGrounded)
        {
            //Debug.Log("IsGrounded" );

            velocity.y = -1.0f;
        }
        else
        {
            //Debug.Log("notGrounded");
            velocity.y += gravity * gravityMult * Time.deltaTime;
        }
        //Debug.Log(velocity.y);

    }
}
