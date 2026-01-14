using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerControllerLocal : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed;
    public float mouseSensitivity;
    public float maxLookAngle;
    public float groundCheckDistance = 0.1f;
    public LayerMask groundLayer = 1;

    public int score = 0;
    public Text txtScore;

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

    public float timeCooldownMax = 10f;
    public float timeCooldown = 0f;

    private bool blinkActive = false;
    private Coroutine blinkRoutine;

    private bool allowCursorLock = true; // se apaga al finalizar la partida

    void OnEnable()
    {
        TimerLocal.onTryStartGame += OnGameStart;
        TimerLocal.onEndGame += OnGameEnd;
    }

    void OnDisable()
    {
        TimerLocal.onTryStartGame -= OnGameStart;
        TimerLocal.onEndGame -= OnGameEnd;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
            meshRenderer.enabled = false;

        playerInput = GetComponent<PlayerInput>();

        allowCursorLock = true;
        ApplyCursorControl(true);

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

    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            ApplyCursorControl(false); // visible
            return;
        }

        ApplyCursorControl(allowCursorLock); // si seguimos controlando, oculto
    }

    void OnApplicationPause(bool pause)
    {
        if (pause)
            ApplyCursorControl(false);
        else
            OnApplicationFocus(Application.isFocused);
    }

    private void OnGameStart()
    {
        allowCursorLock = true;
        ApplyCursorControl(true);
    }

    private void OnGameEnd()
    {
        allowCursorLock = false;
        ApplyCursorControl(false);
    }

    private void ApplyCursorControl(bool controlling)
    {
        if (!Application.isFocused) controlling = false;

        Cursor.lockState = controlling ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !controlling;
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
        if (timeCooldown > 0f) return;
        if (shootScript == null || playerCamera == null || firingPoint == null) return;

        bool hit = shootScript.ShootBullet(playerCamera);
        if (hit)
        {
            ScoreUP();
            if (txtScore != null) txtScore.text = score.ToString();
        }

        if (lineRender != null)
        {
            lineRender.SetPosition(0, firingPoint.transform.position);
            lineRender.SetPosition(1, firingPoint.transform.position + playerCamera.transform.forward * 100f);
        }

        isVisible = true;
        timeCooldown = timeCooldownMax;
        timeToInvisible = timeVisible;

        var hud = GetComponent<PlayerHUD>();
        if (hud != null)
            hud._fReloadUI(timeCooldownMax);
    }

    public delegate void OnScoreUP();
    public static OnScoreUP onScoreUP;

    public void ScoreUP()
    {
        score++;
        var hud = GetComponent<PlayerHUD>();
        if (hud != null) hud._fUpdateScore(score);
        onScoreUP?.Invoke();
    }

    void Update()
    {
        HandleLook();
        CheckGrounded();
        VisibilityHandler();
        ShootCooldown();
    }

    void FixedUpdate()
    {
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
    }

    void VisibilityHandler()
    {
        if (meshRenderer == null) return;

        if (blinkActive)
        {
            if (lineRender != null)
                lineRender.enabled = timeToInvisible > 0f;
            return;
        }

        bool shouldBeVisible = timeToInvisible > 0f;

        meshRenderer.enabled = shouldBeVisible;
        if (lineRender != null) lineRender.enabled = shouldBeVisible;

        var hud = GetComponent<PlayerHUD>();
        if (hud != null) hud._fToggleInvisibilityUI(shouldBeVisible); // ✅ PlayerHUD lo invierte internamente

        isVisible = shouldBeVisible;
        if (timeToInvisible > 0f) timeToInvisible -= Time.deltaTime;
    }

    void ShootCooldown()
    {
        if (timeCooldown > 0f)
            timeCooldown -= Time.deltaTime;
    }

    public void StartDamageBlink()
    {
        if (blinkRoutine != null)
            StopCoroutine(blinkRoutine);

        blinkRoutine = StartCoroutine(DamageBlinkRoutine());
    }

    IEnumerator DamageBlinkRoutine()
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

        bool shouldBeVisible = timeToInvisible > 0f;
        meshRenderer.enabled = shouldBeVisible;
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
