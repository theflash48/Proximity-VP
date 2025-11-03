using Unity.Netcode;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthOnline : NetworkBehaviour
{
    public int maxLives = 2;
    
private NetworkVariable<int> currentLives = new NetworkVariable<int>(
    2,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server
);

// Método público para obtener las vidas actuales
public int GetCurrentLives()
{
    return currentLives.Value;
}
    
    public float respawnDelay = 3f;
    private PlayerControllerLocal playerControllerLocal;
    private Rigidbody rb;
    private Collider playerCollider;
    public Image fadeImage;
    public float fadeDuration = 1f;
    public GameObject deathEffect;
    private bool isDead = false;

    void Start()
    {
        // Solo el servidor inicializa las vidas
        if (IsServer)
        {
            currentLives.Value = maxLives;
        }
        
        playerControllerLocal = GetComponent<PlayerControllerLocal>();
        rb = GetComponent<Rigidbody>();
        playerCollider = GetComponent<Collider>();
        
        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
            fadeImage.gameObject.SetActive(false);
        }
        
        if (SpawnManager.Instance != null)
        {
            SpawnManager.Instance.RegisterPlayer(gameObject);
        }
    }


    public void TakeDamage()
    {
        if (isDead) return;
        
        // Solo el servidor puede aplicar daño
        if (!IsServer) return;

        currentLives.Value--;
        Debug.Log(gameObject.name + " vidas restantes: " + currentLives.Value);

        if (currentLives.Value <= 0)
        {
            Die();
        }
        else
        {
            DamageFlashClientRpc();
        }
    }

    void Die()
    {
        // Solo el servidor ejecuta la muerte
        if (!IsServer) return;

        isDead = true;
        Debug.Log(gameObject.name + " ha muerto!");

        // Ejecutar efectos en todos los clientes
        DieClientRpc();
    }

    [ClientRpc]
    private void DieClientRpc()
    {
        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, Quaternion.identity);
        }

        if (IsOwner && playerControllerLocal != null)
        {
            playerControllerLocal.enabled = false;
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

        // Solo el dueño inicia la secuencia de CARMTNA
        if (IsOwner)
        {
            StartCoroutine(DeathSequence());
        }
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

        yield return new WaitForSeconds(respawnDelay - fadeDuration);

        // El servidor maneja el respawn
        if (IsServer)
        {
            Respawn();
        }

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
        // Solo el servidor puede hacer respawn
        if (!IsServer) return;

        currentLives.Value = maxLives;
        isDead = false;

        // Usar el NetworkSpawnManager para respawn
        if (NetworkSpawnManager.Instance != null)
        {
            NetworkObject netObj = GetComponent<NetworkObject>();
            NetworkSpawnManager.Instance.RespawnPlayer(netObj);
        }
        else
        {
            Transform spawnPoint = null;
            if (SpawnManager.Instance != null)
            {
                spawnPoint = SpawnManager.Instance.GetFarthestSpawnPoint(gameObject);
            }

            if (spawnPoint != null)
            {
                transform.position = spawnPoint.position;
                transform.rotation = spawnPoint.rotation;
            }
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        ReactivatePlayerClientRpc();

        Debug.Log(gameObject.name + " ha respawneado!");
    }

    [ClientRpc]
    private void ReactivatePlayerClientRpc()
    {
        if (playerControllerLocal != null && IsOwner)
        {
            playerControllerLocal.enabled = true;
        }
        if (playerCollider != null)
        {
            playerCollider.enabled = true;
        }
    }

    [ClientRpc]
    private void DamageFlashClientRpc()
    {
        StartCoroutine(DamageFlash());
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
    //Comentado por PABLO, hablado por el grupo. Esta dando fallos y no se si se va a usar
    /*void OnDestroy()
    {
        if (SpawnManager.Instance != null)
        {
            SpawnManager.Instance.UnregisterPlayer(gameObject);
        }
    }*/
}