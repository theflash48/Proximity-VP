using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;
using System.Timers;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : NetworkBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed;
    public float mouseSensitivity;
    public float maxLookAngle;
    public float groundCheckDistance = 0.1f;
    public LayerMask groundLayer = 1;
    public int score = 0;

    Rigidbody rb;
    MeshRenderer meshRenderer;
    PlayerInput playerInput;

    public Camera playerCamera;
    public Transform groundCheck;
    public GameObject firingPoint;
    public Shoot shootScript;

    public Vector2 moveInput;
    public Vector2 lookInput;
    float xRotation = 0f;
    float yRotation = 0f;
    bool isGrounded;
    public bool isVisible = false;
    public float timeVisible = 5f;
    public float timeToInvisible = 0f;

    public GameObject bulletPrefab;

    void Awake()
    {
        //Rigidbody
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true; //Fisicas no afectan la rotacion del player

        //MeshRenderer
        meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.enabled = false;

        //InputSystem
        playerInput = GetComponent<PlayerInput>();

        //Cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        //Shoot
        shootScript = GetComponent<Shoot>();
    }

    // NUEVO: Solo el dueño puede controlar este jugador
    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            // Deshabilitar control para jugadores que no son el dueño
            if (playerInput != null)
            {
                playerInput.enabled = false;
            }
            
            // Deshabilitar la cámara de jugadores remotos
            if (playerCamera != null)
            {
                playerCamera.enabled = false;
                // Opcional: también desactivar el AudioListener
                AudioListener listener = playerCamera.GetComponent<AudioListener>();
                if (listener != null) listener.enabled = false;
            }
        }
    }

    //Cambiamos el OnEnable() y OnDisable() por funciones llamadas por PlayerInput por eventos
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnLook(InputValue value)
    {
        lookInput = value.Get<Vector2>();
    }

    public void OnShoot(InputValue value)
    {
        // Solo el dueño puede disparar
        if (!IsOwner) return;

        Debug.Log("Disparando!");

        // Sistema de disparo con raycast
        shootScript.ShootBullet();
        isVisible = true;

        timeToInvisible = timeVisible;
    }

    private void Update()
    {
        // Solo el dueño ejecuta la lógica local
        if (!IsOwner) return;

        HandleLook();
        CheckGrounded();
        VisibilityHandler();
    }
    
    private void FixedUpdate()
    {
        // Solo el dueño mueve su personaje
        if (!IsOwner) return;

        HandleMovement();
    }

    void HandleMovement()
    {
        // Calcular dirección de movimiento
        Vector3 moveDirection = transform.right * moveInput.x + transform.forward * moveInput.y;

        // Aplicar movimiento al Rigidbody
        Vector3 targetVelocity = moveDirection * moveSpeed;
        targetVelocity.y = rb.linearVelocity.y;

        // Suavizar el movimiento
        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, targetVelocity, Time.fixedDeltaTime * 10f);
    }

    void CheckGrounded()
    {
        if (groundCheck != null)
        {
            isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckDistance, groundLayer);
        }
        else
        {
            // Fallback: raycast hacia abajo desde el centro del jugador
            isGrounded = Physics.Raycast(transform.position, Vector3.down, groundCheckDistance + 0.1f, groundLayer);
        }
    }

    void HandleLook()
    {
        float mouseX = lookInput.x * mouseSensitivity * Time.deltaTime;
        float mouseY = lookInput.y * mouseSensitivity * Time.deltaTime;

        // Rotación vertical (arriba/abajo)
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -maxLookAngle, maxLookAngle);

        //Rotacion horizontal (izquierda(derecha)
        yRotation += mouseX;

        //Rotacion en X
        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        //Rotación horizontal (izquierda/derecha) y mas control en la rotacion en Y
        transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
    }

    void VisibilityHandler()
    {
        if (isVisible) meshRenderer.enabled = true;
        else meshRenderer.enabled = false;
        isVisible = timeToInvisible > 0.0f;
        if (timeToInvisible > 0.0f) timeToInvisible -= Time.deltaTime;
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckDistance);
        }
    }

    // Intenté hacerlo con la corutina pero me daba mejor resultado usar deltaTime como explico arriba
    IEnumerator TurnInvisible()
    {
        yield return new WaitForSeconds(timeVisible);
        isVisible = false;
    }
}