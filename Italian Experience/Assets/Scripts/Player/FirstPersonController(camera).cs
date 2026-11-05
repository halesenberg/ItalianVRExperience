using UnityEngine;

public class FirstPersonController : MonoBehaviour
{
    [Header("Riferimenti")]
    public CharacterController controller;
    public Transform cameraTransform;
    private InputSystem_Actions controls;

    [Header("Impostazioni Movimento")]
    public float walkSpeed = 5f;
    public float gravity = -19.62f;
    public float jumpHeight = 1.5f;

    [Header("Impostazioni Visuale")]
    public float mouseSensitivity = 25f;
    private float xRotation = 0f;

    private Vector3 velocity;
    private bool isGrounded;

    void Awake()
    {
        controls = new InputSystem_Actions();
        // Blocca il mouse al centro dello schermo
        Cursor.lockState = CursorLockMode.Locked;
    }

    void OnEnable() => controls.Enable();
    void OnDisable() => controls.Disable();

    void Update()
    {
        // 1. Ground Check affidabile
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // 2. Rotazione Visuale (Mouse o Levetta DX)
        // Se usi il mouse, il New Input System legge il Delta
        Vector2 lookInput = UnityEngine.InputSystem.Pointer.current != null ?
            UnityEngine.InputSystem.Mouse.current.delta.ReadValue() : Vector2.zero;

        float mouseX = lookInput.x * mouseSensitivity * Time.deltaTime;
        float mouseY = lookInput.y * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f); // Impedisce di ruotare la testa all'indietro

        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);

        // 3. Movimento (WASD / Levetta SX)
        Vector2 moveInput = controls.Player.Move.ReadValue<Vector2>();
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;

        controller.Move(move * walkSpeed * Time.deltaTime);

        // 4. Salto
        if (controls.Player.Jump.WasPerformedThisFrame() && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // 5. Gravità
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}