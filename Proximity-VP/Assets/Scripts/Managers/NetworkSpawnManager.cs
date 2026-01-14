using Unity.Netcode;
using UnityEngine;

public class NetworkSpawnManager : NetworkBehaviour
{
    public static NetworkSpawnManager Instance;

    [Header("Player Settings")]
    public GameObject playerPrefab;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
            SpawnPlayerForClient(clientId);
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
        SpawnPlayerForClient(clientId);
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (!IsServer) return;

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
            Debug.LogError("PlayerPrefab no asignado en NetworkSpawnManager!");
            return;
        }

        if (SpawnManager.Instance == null)
        {
            Debug.LogError("SpawnManager no encontrado en la escena online!");
            return;
        }

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client) &&
            client.PlayerObject != null)
            return;

        Transform spawnPoint = SpawnManager.Instance.GetFarthestSpawnPoint(null);
        if (spawnPoint == null)
        {
            Debug.LogError("No hay spawn point válido.");
            return;
        }

        GameObject playerInstance = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);

        NetworkObject netObj = playerInstance.GetComponent<NetworkObject>();
        if (netObj == null)
        {
            Debug.LogError("El PlayerPrefab NO tiene NetworkObject!");
            Destroy(playerInstance);
            return;
        }

        netObj.SpawnAsPlayerObject(clientId);
        SpawnManager.Instance.RegisterPlayer(playerInstance);
    }
}
