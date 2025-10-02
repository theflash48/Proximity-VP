using System.Collections;
using System.Collections.Generic;
using System.Timers;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed;
    public float mouseSensitivity;
    public float maxLookAngle;
    public float groundCheckDistance = 0.1f;
    public LayerMask groundLayer = 1;

    Rigidbody rb;
    MeshRenderer meshRenderer;
    InputSystem_Actions inputActions;

    public Camera playerCamera;
    public Transform groundCheck;

    public Vector2 moveInput;
    public Vector2 lookInput;
    float xRotation = 0f;
    bool isGrounded;
    public bool isVisible = false;
    public float timeVisible = 5f;
    public float timeToInvisible = 0f;

    public GameObject bulletPrefab;

    void Awake()
    {
        //Rigidbody
        rb = GetComponent<Rigidbody>();

        //MeshRenderer
        meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.enabled = false;

        //InputSystem
        inputActions = new InputSystem_Actions();

        //Cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnEnable()
    {
        // Habilitar el Action Map del Player
        inputActions.Player.Enable();

        // Conectar las acciones directamente
        inputActions.Player.Move.performed += OnMove;
        inputActions.Player.Move.canceled += OnMove;

        inputActions.Player.Look.performed += OnLook;
        inputActions.Player.Look.canceled += OnLook;

        inputActions.Player.Shoot.performed += OnShoot;
    }
    void OnDisable()
    {
        // Desconectar las acciones
        inputActions.Player.Move.performed -= OnMove;
        inputActions.Player.Move.canceled -= OnMove;

        inputActions.Player.Look.performed -= OnLook;
        inputActions.Player.Look.canceled -= OnLook;

        inputActions.Player.Shoot.performed -= OnShoot;

        // Deshabilitar el Action Map
        inputActions.Player.Disable();
    }

    private void Update()
    {
        HandleLook();
        CheckGrounded();
        VisibilityHandler();
    }
    
    private void FixedUpdate()
    {
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

        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Rotación horizontal (izquierda/derecha)
        transform.Rotate(Vector3.up * mouseX);
    }
    // Un timer que siempre que esté por encima de 0 muestra el mesh renderer y reduce el timer el cual es adaptable
    // desde la variable timeVisible, pues cada vez que se dispare se asignará el valor de esa variable a
    // timeToInvisible, al hacer esto, siempre se reinicia el timer cada vez que disparo, sino, el tiempo se volvía
    // irregular el tiempo visible, a revisar si es más optimizable
    void VisibilityHandler()
    {
        if (isVisible) meshRenderer.enabled = true;
        else meshRenderer.enabled = false;
        isVisible = timeToInvisible > 0.0f;
        if (timeToInvisible > 0.0f) timeToInvisible -= Time.deltaTime;
    }
    
    void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }

    void OnShoot(InputAction.CallbackContext context)
    {
        Debug.Log("Disparando!");

        // Sistema de disparo con raycast
        Instantiate(bulletPrefab, playerCamera.transform.position, playerCamera.transform.rotation);
        RaycastHit hit;
        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, 100f))
        {
            isVisible = true;
            Debug.Log("Objeto golpeado: " + hit.transform.name + " en posición: " + hit.point);
        }

        timeToInvisible = timeVisible;
    }

    // Dibujar gizmo para visualizar el ground check
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
