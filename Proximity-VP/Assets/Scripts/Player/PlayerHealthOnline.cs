using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerHealthOnline : NetworkBehaviour
{
    [Header("Health Settings")]
    public int maxLives = 2;
    public NetworkVariable<int> currentLives = new NetworkVariable<int>();

    [Header("Respawn Settings")]
    public float respawnDelay = 3f;

    public TimerLocal timer;

    private Renderer meshRenderer;
    private Collider col;

    void Awake()
    {
        meshRenderer = GetComponent<Renderer>();
        col = GetComponent<Collider>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentLives.Value = maxLives;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc()
    {
        TakeDamage();
    }

    // Solo la lógica del servidor
    private void TakeDamage()
    {
        if (!IsServer) return;

        if (timer != null)
        {
            if (!timer.gameStarted || timer.remainingTime <= 0)
                return;
        }

        currentLives.Value--;
        Debug.Log(gameObject.name + " vidas restantes (online): " + currentLives.Value);

        if (currentLives.Value <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(RespawnCoroutine());
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ResetLivesServerRpc()
    {
        currentLives.Value = maxLives;
    }

    private void Die()
    {
        // aquí podrías hacer animación, etc.
        StartCoroutine(RespawnCoroutine(true));
    }

    private IEnumerator RespawnCoroutine(bool resetLives = false)
    {
        // Desactivar colisión y “render”
        col.enabled = false;
        meshRenderer.enabled = false;

        yield return new WaitForSeconds(respawnDelay);

        // Recolocar en un spawn
        Transform spawn = SpawnManager.Instance != null
            ? SpawnManager.Instance.GetFarthestSpawnPoint(gameObject)
            : null;

        if (spawn != null)
        {
            transform.position = spawn.position;
            transform.rotation = spawn.rotation;
        }

        if (resetLives)
        {
            currentLives.Value = maxLives;
        }

        // Reactivar
        col.enabled = true;
        meshRenderer.enabled = true;

        // Parpadeo de invulnerabilidad (en clientes) 
        BlinkClientRpc();
    }

    [ClientRpc]
    void BlinkClientRpc()
    {
        StartCoroutine(BlinkCoroutine());
    }

    IEnumerator BlinkCoroutine()
    {
        if (meshRenderer == null) yield break;

        for (int i = 0; i < 10; i++)
        {
            meshRenderer.enabled = !meshRenderer.enabled;
            yield return new WaitForSeconds(0.1f);
        }

        meshRenderer.enabled = true;
    }

    void OnDestroy()
    {
        if (SpawnManager.Instance != null)
        {
            SpawnManager.Instance.UnregisterPlayer(gameObject);
        }
    }
}
