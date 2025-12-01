using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;
using System.Timers;
using UnityEngine;
using UnityEngine.InputSystem;

// Cambiado nombre del archivo/clase a PlayerControllerOnline ya que el
// otro se llamaba PlayerControllerLocal y este PlayerController, solo
// por consistencia de nombres, por lo que se ha actualizado en el resto
// de scripts 
public class PlayerControllerOnline : NetworkBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed;
    public float mouseSensitivity;
    public float maxLookAngle;
    public float groundCheckDistance = 0.1f;
    public LayerMask groundLayer = 1;
    public int score = 0;
    public delegate void OnScoreUPOnline();
public static event OnScoreUPOnline onScoreUPOnline;


    Rigidbody rb;
    MeshRenderer meshRenderer;
    PlayerInput playerInput;

    public GameObject playerCamera;
    public Camera cameraComponent;
    public Transform groundCheck;
    public GameObject firingPoint;
    public Shoot shootScript;
    public LineRenderer lineRender;
    
    

    public Vector2 moveInput;
    public Vector2 lookInput;
    float xRotation = 0f;
    float yRotation = 0f;
    bool isGrounded;
    public bool isVisible = false;
    public float timeVisible = 5f;
    public float timeToInvisible = 0f;

    void Awake()
    {
        //Rigidbody
        rb = GetComponent<Rigidbody>();
        //rb.freezeRotation = true; //Fisicas no afectan la rotacion del player

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
        lineRender = firingPoint.GetComponent<LineRenderer>();
        lineRender.enabled = false;
        lineRender.useWorldSpace = true;
        lineRender.positionCount = 2;
    }

    public void ScoreUP()
{
    score++;

    var hud = GetComponent<PlayerHUD>();
    if (hud != null)
    {
        hud._fUpdateScore(score);
    }

    onScoreUPOnline?.Invoke();
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
            if (cameraComponent != null)
            {
                cameraComponent.enabled = false;
                // Opcional: también desactivar el AudioListener
                AudioListener listener = cameraComponent.GetComponent<AudioListener>();
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
    if (shootScript == null || playerCamera == null) return;

    Debug.Log("Disparando!");

    bool hitPlayer = shootScript.ShootBullet(playerCamera);
    if (hitPlayer)
    {
        ScoreUP();
    }

    if (lineRender != null && firingPoint != null)
    {
        lineRender.SetPosition(0, firingPoint.transform.position);
        lineRender.SetPosition(1, firingPoint.transform.position + playerCamera.transform.forward * 100);
    }

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
        cameraComponent.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        //Rotación horizontal (izquierda/derecha) y mas control en la rotacion en Y
        transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
    }

    void VisibilityHandler()
    {
        if (isVisible)
        {
            meshRenderer.enabled = true;
            lineRender.enabled = true;
        }
        else
        {
            meshRenderer.enabled = false;
            lineRender.enabled = false;
        }
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
}