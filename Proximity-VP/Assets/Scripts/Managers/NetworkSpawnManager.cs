using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Spawnea jugadores al entrar en la escena online.
/// Host = servidor, clientes se conectan y se instancia un PlayerOnline para cada uno.
/// </summary>
public class NetworkSpawnManager : NetworkBehaviour
{
    public static NetworkSpawnManager Instance;

    [Header("Player Settings")]
    [Tooltip("Prefab del jugador online (con NetworkObject)")]
    public GameObject playerPrefab;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    //-------------------------------------------------------------------
    // CUANDO ESTA ESCENA APARECE, SE SPAWNEA UN PLAYER PARA CADA CLIENTE
    //-------------------------------------------------------------------
    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        // Para clientes que se conecten DESPUÉS
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

        // Para los que YA estaban conectados antes de cargar la escena
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            SpawnPlayerForClient(clientId);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer) return;

        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    //-------------------------------------------------------------------
    // MANEJO DE NUEVOS CLIENTES
    //-------------------------------------------------------------------
    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Cliente {clientId} se ha conectado. Spawneando jugador...");
        SpawnPlayerForClient(clientId);
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"Cliente {clientId} se ha desconectado.");
        // Aquí podrías destruir su PlayerObject, si quieres
    }

    //-------------------------------------------------------------------
    // MÉTODO PRINCIPAL DE SPAWN
    //-------------------------------------------------------------------
    private void SpawnPlayerForClient(ulong clientId)
    {
        if (!IsServer)
        {
            Debug.LogError("Solo el servidor puede spawnear jugadores!");
            return;
        }

        if (playerPrefab == null)
        {
            Debug.LogError("PlayerPrefab no asignado en NetworkSpawnManager!");
            return;
        }

        if (SpawnManager.Instance == null)
        {
            Debug.LogError("SpawnManager no encontrado en GameOnline!");
            return;
        }

        // Evitar doble spawn si ya existe uno para ese cliente
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
        {
            if (client.PlayerObject != null)
            {
                Debug.Log($"Cliente {clientId} YA tenía PlayerObject. No se crea otro.");
                return;
            }
        }

        Transform spawnPoint = SpawnManager.Instance.GetFarthestSpawnPoint(null);
        if (spawnPoint == null)
        {
            Debug.LogError("No se encontró spawn point!");
            return;
        }

        // Crear jugador
        GameObject playerInstance = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
        SetupPlayerReferences(playerInstance);

        // 🔥 FIX PANTALLA NEGRA (registrar jugador en SpawnManager)
        SpawnManager.Instance.RegisterPlayer(playerInstance);

        NetworkObject networkObject = playerInstance.GetComponent<NetworkObject>();
        if (networkObject == null)
        {
            Debug.LogError("El PlayerPrefab NO tiene NetworkObject!");
            Destroy(playerInstance);
            return;
        }

        networkObject.SpawnAsPlayerObject(clientId);

        Debug.Log($"Jugador {clientId} spawneado en {spawnPoint.position}");
    }

    //-------------------------------------------------------------------
    // AJUSTE AUTOMÁTICO DEL FIRINGPOINT Y CÁMARA
    //-------------------------------------------------------------------
    private void SetupPlayerReferences(GameObject playerInstance)
    {
        PlayerControllerOnline player = playerInstance.GetComponent<PlayerControllerOnline>();
        if (player == null) return;

        // Auto detectar cámara si no está asignada
        if (player.cameraComponent == null)
            player.cameraComponent = playerInstance.GetComponentInChildren<Camera>(true);

        if (player.cameraComponent != null)
            player.playerCamera = player.cameraComponent.gameObject;

        // FiringPoint
        Transform fp = playerInstance.transform.Find("FiringPoint");

        if (fp == null)
        {
            if (player.playerCamera != null)
            {
                GameObject firing = new GameObject("FiringPoint");
                firing.transform.SetParent(player.playerCamera.transform);
                firing.transform.localPosition = Vector3.forward * 0.5f;
                firing.transform.localRotation = Quaternion.identity;

                player.firingPoint = firing;
                Debug.Log("FiringPoint generado automáticamente");
            }
        }
        else
        {
            player.firingPoint = fp.gameObject;
        }
    }

    //-------------------------------------------------------------------
    // RESPWAN OPCIONAL DESDE EL SERVIDOR
    //-------------------------------------------------------------------
    public void RespawnPlayer(NetworkObject playerNetworkObject)
    {
        if (!IsServer)
        {
            Debug.LogError("Solo el servidor puede respawnear jugadores!");
            return;
        }

        Transform spawnPoint = SpawnManager.Instance.GetFarthestSpawnPoint(playerNetworkObject.gameObject);
        if (spawnPoint != null)
        {
            playerNetworkObject.transform.position = spawnPoint.position;
            playerNetworkObject.transform.rotation = spawnPoint.rotation;

            Debug.Log($"Respawn de player {playerNetworkObject.OwnerClientId} en {spawnPoint.position}");
        }
    }
}
