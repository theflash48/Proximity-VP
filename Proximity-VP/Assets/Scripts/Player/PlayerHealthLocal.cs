using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthLocal : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxLives = 2;
    public int currentLives;

    [Header("Respawn Settings")]
    public float respawnDelay = 3f;

    public GameObject gameManager;
    public TimerLocal timer;

    private PlayerControllerLocal playerController;
    private Rigidbody rb;
    private Collider playerCollider;

    [Header("Death Screen")]
    public Image fadeImage;
    public float fadeDuration = 1f;

    [Header("Visual Feedback")]
    public GameObject deathEffect;

    private bool isDead = false;

    void Start()
    {
        currentLives = maxLives;

        playerController = GetComponent<PlayerControllerLocal>();
        rb = GetComponent<Rigidbody>();
        playerCollider = GetComponent<Collider>();

        gameManager = GameObject.Find("GameManager");
        if (gameManager != null)
            timer = gameManager.GetComponent<TimerLocal>();

        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
            fadeImage.gameObject.SetActive(false);
        }

        if (SpawnManager.Instance != null)
            SpawnManager.Instance.RegisterPlayer(gameObject);
    }

    public void TakeDamage()
    {
        if (timer == null) return;
        if (!timer.gameStarted || timer.remainingTime <= 0f) return;
        if (isDead) return;

        currentLives--;
        var hud = GetComponent<PlayerHUD>();
        if (hud != null) hud._fHealthUI(currentLives, maxLives);

        if (currentLives <= 0)
        {
            Die();
        }
        else
        {
            if (playerController != null)
                playerController.StartDamageBlink();
        }
    }

    void Die()
    {
        isDead = true;

        if (deathEffect != null)
            Instantiate(deathEffect, transform.position, Quaternion.identity);

        if (playerController != null)
            playerController.enabled = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        var mr = GetComponent<MeshRenderer>();
        if (mr != null) mr.enabled = false;

        if (playerCollider != null)
            playerCollider.enabled = false;

        StartCoroutine(DeathSequence());
    }

    IEnumerator DeathSequence()
    {
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            float elapsedTime = 0f;
            Color c = fadeImage.color;

            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.deltaTime;
                c.a = Mathf.Lerp(0f, 1f, elapsedTime / fadeDuration);
                fadeImage.color = c;
                yield return null;
            }

            c.a = 1f;
            fadeImage.color = c;
        }

        yield return new WaitForSeconds(Mathf.Max(0.05f, respawnDelay - fadeDuration));

        Respawn();

        if (fadeImage != null)
        {
            float elapsedTime = 0f;
            Color c = fadeImage.color;

            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.deltaTime;
                c.a = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);
                fadeImage.color = c;
                yield return null;
            }

            c.a = 0f;
            fadeImage.color = c;
            fadeImage.gameObject.SetActive(false);
        }
    }

    void Respawn()
    {
        currentLives = maxLives;

        var hud = GetComponent<PlayerHUD>();
        if (hud != null) hud._fHealthUI(currentLives, maxLives);

        isDead = false;

        Transform spawnPoint = null;
        if (SpawnManager.Instance != null)
            spawnPoint = SpawnManager.Instance.GetFarthestSpawnPoint(gameObject);

        if (spawnPoint != null)
        {
            transform.position = spawnPoint.position;
            transform.rotation = spawnPoint.rotation;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (playerController != null)
            playerController.enabled = true;

        if (playerCollider != null)
            playerCollider.enabled = true;

        if (playerController != null)
            playerController.StartDamageBlink();
    }

    void OnDestroy()
    {
        if (SpawnManager.Instance != null)
            SpawnManager.Instance.UnregisterPlayer(gameObject);
    }
}
