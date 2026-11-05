using UnityEngine;

public class PlayerFreeMovement : MonoBehaviour
{
    [Header("Riferimenti")]
    public CharacterController controller;
    private InputSystem_Actions controls;
    private Animator animator;

    [Header("Parametri Movimento")]
    public float speed = 5f;            // Velocità moderata per evitare scatti
    public float rotationSpeed = 150f;  // Rotazione più fluida
    public float jumpHeight = 1.5f;
    public float gravity = -20f;        // Gravità decisa per stare a terra

    private Vector3 velocity;
    private bool isGrounded;

    void Awake()
    {
        controls = new InputSystem_Actions();
    }

    void OnEnable() => controls.Enable();
    void OnDisable() => controls.Disable();

    void Start()
    {
        if (controller == null)
            controller = GetComponent<CharacterController>();

        animator = GetComponent<Animator>();

        if (animator != null)
            animator.applyRootMotion = false;
    }

    void Update()
    {
        //Controllo stabile del terreno
        isGrounded = controller.isGrounded;

        if (isGrounded && velocity.y < 0)
        {
            //ancora player a terra
            velocity.y = -2f;
        }

        //Lettura Input
        Vector2 moveInput = controls.Player.Move.ReadValue<Vector2>();
        float turnInput = moveInput.x;
        float moveInputY = moveInput.y;

        //Rotazione (Frecce DX/SX)
        if (Mathf.Abs(turnInput) > 0.01f)
        {
            transform.Rotate(0, turnInput * rotationSpeed * Time.deltaTime, 0);
        }

        // Movimento Orizzontale (Frecce SU/GIÙ)
        // vettore basato solo sulla direzione avanti del player
        Vector3 moveDir = transform.forward * moveInputY;

        // impediamo al movimento di influenzare l'altezza (Y)
        moveDir.y = 0;

        // Applichiamo il movimento orizzontale
        controller.Move(moveDir.normalized * speed * Time.deltaTime);

        // Salto
        if (controls.Player.Jump.WasPerformedThisFrame() && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            Debug.Log("Io salto!");
        }

        // Gravità (Movimento Verticale)
        velocity.y += gravity * Time.deltaTime;

        // Applichiamo la gravità separatamente
        controller.Move(velocity * Time.deltaTime);

        // Animazioni
    
        if (animator != null)
        {
            // Verifichiamo se il player sta effettivamente premendo avanti o dietro
            // Usiamo solo moveInputY così se ruota sul posto non sembra che cammini
            bool staCamminando = Mathf.Abs(moveInputY) > 0.1f;

            // Comunichiamo il valore al parametro dell'Animator
            animator.SetBool("isRunning", staCamminando);
        }
    }
}