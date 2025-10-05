using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    public int maxLives = 2;
    public int currentLives;
    public float respawnDelay = 3f;
    private PlayerController playerController;
    private Rigidbody rb;
    private Collider playerCollider;
    public Image fadeImage; // CARTMAN
    public float fadeDuration = 1f;
    public GameObject deathEffect; // Efecto de partículas al morir (opcional)
    private bool isDead = false;
    private GameObject shooter; // Referencia a quien disparó la bala

    void Start()
    {
        currentLives = maxLives;
        playerController = GetComponent<PlayerController>();
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

    void OnTriggerEnter(Collider other) // A revisar por diseño, o matamos por raycast o matamos por bala,
                                        // pero no las 2 porque está buggenado el sistema de puntuacion
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
        if (isDead) return;

        currentLives--;
        Debug.Log(gameObject.name + " vidas restantes: " + currentLives);

        if (currentLives <= 0)
        {
            Die();
        }
        else
        {
            // Feedback visual de hit (opcional)
            StartCoroutine(DamageFlash());
        }
    }

    void Die()
    {
        isDead = true;
        Debug.Log(gameObject.name + " ha muerto!");

        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, Quaternion.identity);
        }

        if (playerController != null)
        {
            playerController.enabled = false;
        }
        
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.enabled = false;
        }

        if (playerCollider != null)
        {
            playerCollider.enabled = false;
        }
        StartCoroutine(DeathSequence());
    }

    IEnumerator DeathSequence()
    {
        // Fade a CARTMAN
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

        // Esperar un momento con CARTMna
        yield return new WaitForSeconds(respawnDelay - fadeDuration);

        // Respawnear
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
        // Restaurar vidas
        currentLives = maxLives;
        isDead = false;

        // Obtener spawn point mas lejano :3
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
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        if (playerController != null)
        {
            playerController.enabled = true;
        }
        if (playerCollider != null)
        {
            playerCollider.enabled = true;
        }

        Debug.Log(gameObject.name + " ha respawneado!");
    }

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
        if (SpawnManager.Instance != null)
        {
            SpawnManager.Instance.UnregisterPlayer(gameObject);
        }
    }
}