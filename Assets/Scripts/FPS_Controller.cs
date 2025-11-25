using System.Linq.Expressions;
using UnityEngine;

public class FPS_Controller : MonoBehaviour
{
    [SerializeField] private float speed;
    [SerializeField] private float sensitivity;
    [SerializeField] private float jumpHeight;
    private float gravity = -9.81f;

    // Camera movement Axis
    private float mouseY;
    private float mouseX;
    [SerializeField] private float maxLook; // Max camera movement (up)
    [SerializeField] private float minLook; // Min camera movement (down)

    private float crouchingTime = 2f;
    private float crouchSpeedMult = 0.4f;
    private float crouchHeight = 0.3f;
    private float normalHeight = 1f;
    private float offset;

    [SerializeField] private Transform player;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private CharacterController controller;
    private Vector3 velocity;

    private bool crouching = false;
    private bool isGrounded = true;

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

        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");
        Vector3 move = transform.right * moveX + transform.forward * moveZ;

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

        velocity.y += gravity * Time.deltaTime;

        #region Crouching
        if (!crouching)
        {
            controller.Move(move * speed * Time.deltaTime);
        }

        if (crouching)
        {
            controller.Move(move * (speed * crouchSpeedMult) * Time.deltaTime);
        }

        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            Debug.Log("Crouched");
            crouching = !crouching;
        }

        if (crouching == true)
        {
            controller.height = controller.height - crouchingTime * Time.deltaTime;
            if (controller.height <= crouchHeight)
            {
                controller.height = crouchHeight;
            }
        }

        if (crouching == false)
        {
            controller.height = controller.height + crouchingTime * Time.deltaTime;

            if (controller.height >= normalHeight)
            {
                controller.height = normalHeight;
            }
        }
        #endregion


    }
}
