using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkSpawnManager : NetworkBehaviour
{
    public static NetworkSpawnManager Instance;

    [Header("Player Settings")]
    public GameObject playerPrefab;

    // Evita duplicados y permite esperar a que haya spawns listos
    private readonly HashSet<ulong> _spawnInProgress = new HashSet<ulong>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

        // Spawnear los que ya estén conectados (incluye host)
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
            QueueSpawnForClient(clientId);
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer) return;

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        QueueSpawnForClient(clientId);
    }

    private void QueueSpawnForClient(ulong clientId)
    {
        if (!IsServer) return;

        // Si ya tiene PlayerObject, no hacemos nada
        if (NetworkManager.Singleton != null &&
            NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client) &&
            client.PlayerObject != null)
            return;

        if (_spawnInProgress.Contains(clientId))
            return;

        StartCoroutine(SpawnWhenReady(clientId));
    }

    private IEnumerator SpawnWhenReady(ulong clientId)
    {
        _spawnInProgress.Add(clientId);

        // Espera a que exista SpawnManager y tenga spawns detectados
        float timeout = 5f;
        float t = 0f;

        while (!IsSpawnSystemReady())
        {
            t += Time.deltaTime;
            if (t >= timeout)
            {
                Debug.LogWarning("NetworkSpawnManager: timeout esperando spawn system. Reintento igualmente.");
                break;
            }
            yield return null;
        }

        SpawnPlayerForClient(clientId);

        _spawnInProgress.Remove(clientId);
    }

    private bool IsSpawnSystemReady()
    {
        if (SpawnManager.Instance == null) return false;

        // Si está vacío, intentamos auto-descubrir (por si mapa aleatorio acaba de entrar)
        if (SpawnManager.Instance.spawnPoints == null || SpawnManager.Instance.spawnPoints.Length == 0)
            SpawnManager.Instance.AutoDiscoverSpawnPoints();

        return SpawnManager.Instance.spawnPoints != null && SpawnManager.Instance.spawnPoints.Length > 0;
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (!IsServer) return;

        // Si estaba esperando spawn, lo quitamos
        _spawnInProgress.Remove(clientId);

        if (NetworkManager.Singleton != null &&
            NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client) &&
            client.PlayerObject != null)
        {
            var go = client.PlayerObject.gameObject;

            if (SpawnManager.Instance != null)
                SpawnManager.Instance.UnregisterPlayer(go);

            client.PlayerObject.Despawn(true);
        }
    }

    private void SpawnPlayerForClient(ulong clientId)
    {
        if (!IsServer) return;

        if (playerPrefab == null)
        {
            Debug.LogError("NetworkSpawnManager: PlayerPrefab no asignado!");
            return;
        }

        if (SpawnManager.Instance == null)
        {
            Debug.LogError("NetworkSpawnManager: SpawnManager no encontrado en la escena online!");
            return;
        }

        // Doble check: por si le spawnearon entre medias
        if (NetworkManager.Singleton != null &&
            NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client) &&
            client.PlayerObject != null)
            return;

        // Asegurar spawns listos
        if (SpawnManager.Instance.spawnPoints == null || SpawnManager.Instance.spawnPoints.Length == 0)
            SpawnManager.Instance.AutoDiscoverSpawnPoints();

        Transform spawnPoint = SpawnManager.Instance.GetFarthestSpawnPoint(null);
        if (spawnPoint == null)
        {
            Debug.LogError("NetworkSpawnManager: No hay spawn point válido.");
            return;
        }

        GameObject playerInstance = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);

        NetworkObject netObj = playerInstance.GetComponent<NetworkObject>();
        if (netObj == null)
        {
            Debug.LogError("NetworkSpawnManager: El PlayerPrefab NO tiene NetworkObject!");
            Destroy(playerInstance);
            return;
        }

        netObj.SpawnAsPlayerObject(clientId);
        SpawnManager.Instance.RegisterPlayer(playerInstance);
    }
}
