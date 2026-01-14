using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerControllerOnline : NetworkBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed;
    public float mouseSensitivity;
    public float maxLookAngle;
    public float groundCheckDistance = 0.1f;
    public LayerMask groundLayer = 1;

    [Header("Reveal Settings")]
    public float timeVisible = 2f;

    [Header("Cooldown")]
    public float timeCooldownMax = 0.5f;
    public float timeCooldown = 0f;

    [Header("Score")]
    public int score = 0;

    public delegate void OnScoreUPOnline();
    public static event OnScoreUPOnline onScoreUPOnline;

    [Header("References")]
    public GameObject playerCamera;
    public Camera cameraComponent;
    public Transform groundCheck;
    public GameObject firingPoint;
    public Shoot shootScript;
    public LineRenderer lineRender;

    [Header("Inputs")]
    public Vector2 moveInput;
    public Vector2 lookInput;

    private Rigidbody rb;
    private MeshRenderer meshRenderer;
    private PlayerInput playerInput;
    private PlayerHUD hud;
    private PlayerHealthOnline health;

    private float xRotation = 0f;
    private float yRotation = 0f;
    private bool isGrounded;

    private Coroutine tracerRoutine;

    private bool blinkActive;
    private Coroutine blinkRoutine;

    private NetworkVariable<int> scoreNet = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private NetworkVariable<float> pitchNet = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private NetworkVariable<double> revealUntil = new NetworkVariable<double>(
        0d, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        meshRenderer = GetComponent<MeshRenderer>();
        playerInput = GetComponent<PlayerInput>();
        hud = GetComponent<PlayerHUD>();
        health = GetComponent<PlayerHealthOnline>();

        if (meshRenderer != null)
            meshRenderer.enabled = false;

        if (cameraComponent == null)
            cameraComponent = GetComponentInChildren<Camera>(true);

        if (cameraComponent != null && playerCamera == null)
            playerCamera = cameraComponent.gameObject;

        if (shootScript == null)
            shootScript = GetComponent<Shoot>();

        if (lineRender == null && firingPoint != null)
            lineRender = firingPoint.GetComponent<LineRenderer>();

        if (lineRender != null)
        {
            lineRender.enabled = false;
            lineRender.useWorldSpace = true;
            lineRender.positionCount = 2;
        }
    }

    public override void OnNetworkSpawn()
    {
        if (playerInput != null)
            playerInput.enabled = IsOwner;

        if (cameraComponent != null)
        {
            cameraComponent.enabled = true;

            var listener = cameraComponent.GetComponent<AudioListener>();
            if (listener != null)
                listener.enabled = IsOwner;

            if (playerCamera != null)
                playerCamera.SetActive(true);
        }

        if (IsOwner)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        score = scoreNet.Value;
        if (hud != null)
            hud._fUpdateScore(score);

        scoreNet.OnValueChanged += OnScoreChanged;
    }

    public override void OnNetworkDespawn()
    {
        scoreNet.OnValueChanged -= OnScoreChanged;
    }

    private void OnScoreChanged(int previous, int current)
    {
        score = current;

        if (hud != null)
            hud._fUpdateScore(score);

        onScoreUPOnline?.Invoke();
    }

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnLook(InputValue value)
    {
        if (playerInput != null && playerInput.currentControlScheme == "Keyboard&Mouse")
            mouseSensitivity = 10f;
        else
            mouseSensitivity = 100f;

        lookInput = value.Get<Vector2>();
    }

    public void OnShoot(InputValue value)
    {
        if (!IsOwner) return;
        if (timeCooldown > 0f) return;

        if (playerCamera == null) return;

        // Sonido/feedback local (sin daño online desde aquí)
        if (shootScript != null)
            shootScript.ShootBullet(playerCamera);

        Vector3 origin = firingPoint != null ? firingPoint.transform.position : playerCamera.transform.position;
        Vector3 dir = playerCamera.transform.forward;

        ShootServerRpc(origin, dir, timeVisible);

        timeCooldown = timeCooldownMax;
        if (hud != null)
            hud._fReloadUI(timeCooldownMax);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ShootServerRpc(Vector3 origin, Vector3 dir, float visibleDuration, ServerRpcParams rpcParams = default)
    {
        if (NetworkManager.Singleton == null) return;

        // Reveal del shooter (este mismo player)
        double now = NetworkManager.Singleton.ServerTime.Time;
        revealUntil.Value = now + Mathf.Max(0f, visibleDuration);

        Vector3 end = origin + dir * 100f;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, 100f))
        {
            end = hit.point;

            var target = hit.collider.GetComponentInParent<PlayerHealthOnline>();
            if (target != null && target.OwnerClientId != rpcParams.Receive.SenderClientId)
            {
                target.ApplyDamageFromServer(rpcParams.Receive.SenderClientId);
            }
        }

        ShootVfxClientRpc(origin, end);
    }

    [ClientRpc]
    private void ShootVfxClientRpc(Vector3 start, Vector3 end)
    {
        if (lineRender == null) return;

        lineRender.SetPosition(0, start);
        lineRender.SetPosition(1, end);

        if (tracerRoutine != null)
            StopCoroutine(tracerRoutine);

        tracerRoutine = StartCoroutine(TracerFlash());
    }

    private IEnumerator TracerFlash()
    {
        if (lineRender == null) yield break;

        lineRender.enabled = true;
        yield return new WaitForSeconds(0.05f);
        lineRender.enabled = false;
    }

    void Update()
    {
        ApplyRemotePitch();
        ApplyVisibility();

        if (!IsOwner) return;

        HandleLook();
        CheckGrounded();
        ShootCooldown();
    }

    void FixedUpdate()
    {
        if (!IsOwner) return;
        HandleMovement();
    }

    void HandleMovement()
    {
        Vector3 moveDirection = transform.right * moveInput.x + transform.forward * moveInput.y;
        Vector3 targetVelocity = moveDirection * moveSpeed;

        if (rb != null)
        {
            targetVelocity.y = rb.linearVelocity.y;
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, targetVelocity, Time.fixedDeltaTime * 10f);
        }
    }

    void CheckGrounded()
    {
        if (groundCheck != null)
            isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckDistance, groundLayer);
        else
            isGrounded = Physics.Raycast(transform.position, Vector3.down, groundCheckDistance + 0.1f, groundLayer);
    }

    void HandleLook()
    {
        if (cameraComponent == null) return;

        float mouseX = lookInput.x * mouseSensitivity * Time.deltaTime;
        float mouseY = lookInput.y * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -maxLookAngle, maxLookAngle);

        yRotation += mouseX;

        cameraComponent.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.rotation = Quaternion.Euler(0f, yRotation, 0f);

        pitchNet.Value = xRotation;
    }

    private void ApplyRemotePitch()
    {
        if (cameraComponent == null) return;

        if (!IsOwner)
            cameraComponent.transform.localRotation = Quaternion.Euler(pitchNet.Value, 0f, 0f);
    }

    private void ApplyVisibility()
    {
        if (meshRenderer == null) return;

        if (health != null && health.IsRespawning)
        {
            meshRenderer.enabled = false;
            if (hud != null) hud._fToggleInvisibilityUI(false);
            return;
        }

        if (blinkActive) return;

        bool revealed = false;
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            revealed = NetworkManager.Singleton.ServerTime.Time < revealUntil.Value;

        meshRenderer.enabled = revealed;

        if (hud != null)
            hud._fToggleInvisibilityUI(revealed);
    }

    void ShootCooldown()
    {
        if (timeCooldown > 0f)
            timeCooldown -= Time.deltaTime;
    }

    // Llamado por PlayerHealthOnline (server) para otorgar kills
    public void AddScoreServer(int amount)
    {
        if (!IsServer) return;
        scoreNet.Value += Mathf.Max(0, amount);
    }

    // Llamado por PlayerHealthOnline (ClientRpc) para blink
    public void StartDamageBlink()
    {
        if (blinkRoutine != null)
            StopCoroutine(blinkRoutine);

        blinkRoutine = StartCoroutine(DamageBlinkRoutine());
    }

    private IEnumerator DamageBlinkRoutine()
    {
        if (meshRenderer == null) yield break;

        blinkActive = true;

        meshRenderer.enabled = true;
        yield return new WaitForSeconds(0.1f);
        meshRenderer.enabled = false;
        yield return new WaitForSeconds(0.1f);
        meshRenderer.enabled = true;
        yield return new WaitForSeconds(0.1f);
        meshRenderer.enabled = false;
        yield return new WaitForSeconds(0.1f);

        blinkActive = false;
        ApplyVisibility();
    }
}
