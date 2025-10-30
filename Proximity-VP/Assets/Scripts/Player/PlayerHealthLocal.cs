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
    
    [Header("References")]
    private PlayerControllerLocal playerController;
    private Rigidbody rb;
    private Collider playerCollider;
    
    [Header("Death Screen")]
    public Image fadeImage; // Asigna una UI Image negra que cubra toda la cámara del jugador
    public float fadeDuration = 1f;
    
    [Header("Visual Feedback")]
    public GameObject deathEffect; // Efecto de partículas al morir (opcional)
    
    private bool isDead = false;
    private GameObject shooter; // Referencia a quien disparó la bala

    void Start()
    {
        currentLives = maxLives;
        playerController = GetComponent<PlayerControllerLocal>();
        rb = GetComponent<Rigidbody>();
        playerCollider = GetComponent<Collider>();
        
        // Asegurarse de que la pantalla empiece transparente
        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
            fadeImage.gameObject.SetActive(false);
        }
        
        // Registrar jugador en el SpawnManager
        if (SpawnManager.Instance != null)
        {
            SpawnManager.Instance.RegisterPlayer(gameObject);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Si es golpeado por una bala
        if (other.CompareTag("Bullet") && !isDead)
        {
            // Verificar que la bala no sea del mismo jugador
            BulletScript bullet = other.GetComponent<BulletScript>();
            if (bullet != null && bullet.owner != gameObject)
            {
                TakeDamage();
            }
        }
    }

    public void TakeDamage()
    {
        currentLives--;
        Debug.Log(gameObject.name + " vidas restantes: " + currentLives);

        if (currentLives <= 0)
        {
            Die();
        }
        else
        {
            // Feedback visual de daño (opcional)
            StartCoroutine(DamageFlash());
        }
    }

    void Die()
    {
        isDead = true;
        Debug.Log(gameObject.name + " ha muerto!");

        // Efecto de muerte
        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, Quaternion.identity);
        }

        // Deshabilitar controles y físicas
        if (playerController != null)
        {
            playerController.enabled = false;
        }
        
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Ocultar el jugador
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.enabled = false;
        }

        // Deshabilitar colisiones
        if (playerCollider != null)
        {
            playerCollider.enabled = false;
        }

        // Iniciar fade a negro y respawn
        StartCoroutine(DeathSequence());
    }

    IEnumerator DeathSequence()
    {
        // Fade a negro
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

        // Esperar un momento con pantalla negra
        yield return new WaitForSeconds(respawnDelay - fadeDuration);

        // Respawnear
        Respawn();

        // Fade desde negro a transparente
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
        // Restaurar vidas
        currentLives = maxLives;
        isDead = false;

        // Obtener spawn point más lejano
        Transform spawnPoint = null;
        if (SpawnManager.Instance != null)
        {
            spawnPoint = SpawnManager.Instance.GetFarthestSpawnPoint(gameObject);
        }

        // Mover jugador al spawn point
        if (spawnPoint != null)
        {
            transform.position = spawnPoint.position;
            transform.rotation = spawnPoint.rotation;
        }

        // Resetear físicas
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Reactivar controles
        if (playerController != null)
        {
            playerController.enabled = true;
        }

        // Reactivar colisiones
        if (playerCollider != null)
        {
            playerCollider.enabled = true;
        }

        Debug.Log(gameObject.name + " ha respawneado!");
    }

    // Efecto visual de daño (parpadeo)
    IEnumerator DamageFlash()
    {
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            for (int i = 0; i < 3; i++)
            {
                meshRenderer.enabled = false;
                yield return new WaitForSeconds(0.1f);
                meshRenderer.enabled = true;
                yield return new WaitForSeconds(0.1f);
            }
        }
    }

    void OnDestroy()
    {
        // Desregistrar jugador del SpawnManager
        if (SpawnManager.Instance != null)
        {
            SpawnManager.Instance.UnregisterPlayer(gameObject);
        }
    }
}