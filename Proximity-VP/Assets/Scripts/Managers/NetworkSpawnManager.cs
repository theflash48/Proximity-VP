using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Maneja el spawn automático de jugadores en red cuando se conectan.
/// Usa el SpawnManager existente para determinar el mejor punto de spawn.
/// </summary>
public class NetworkSpawnManager : NetworkBehaviour
{
    public static NetworkSpawnManager Instance;

    [Header("Player Settings")]
    [Tooltip("Prefab del jugador que se spawneará (debe estar en Network Prefabs del NetworkManager)")]
    public GameObject playerPrefab;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Cliente {clientId} conectado. Spawneando jugador...");
        SpawnPlayerForClient(clientId);
    }
    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"Cliente {clientId} desconectado.");
    }

    private void SpawnPlayerForClient(ulong clientId)
    {
        if (!IsServer)
        {
            Debug.LogError("Solo el servidor puede spawnear jugadores!");
            return;
        }

        if (playerPrefab == null)
        {
            Debug.LogError("Player Prefab no asignado en NetworkSpawnManager!");
            return;
        }

        if (SpawnManager.Instance == null)
        {
            Debug.LogError("SpawnManager no encontrado en la escena!");
            return;
        }

        Transform spawnPoint = SpawnManager.Instance.GetFarthestSpawnPoint(null);

        if (spawnPoint == null)
        {
            Debug.LogError("No se pudo obtener un spawn point!");
            return;
        }

        GameObject playerInstance = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
        SetupPlayerReferences(playerInstance);
        NetworkObject networkObject = playerInstance.GetComponent<NetworkObject>();

        if (networkObject == null)
        {
            Debug.LogError("El Player Prefab no tiene un componente NetworkObject!");
            Destroy(playerInstance);
            return;
        }
        networkObject.SpawnAsPlayerObject(clientId);

        Debug.Log($"Jugador spawneado para cliente {clientId} en posición {spawnPoint.position}");
    }

    private void SetupPlayerReferences(GameObject playerInstance)
    {
        PlayerControllerOnline playerController = playerInstance.GetComponent<PlayerControllerOnline>();
        
        if (playerController != null)
        {
            Transform firingPointTransform = playerInstance.transform.Find("FiringPoint");
            
            if (firingPointTransform == null)
            {
                GameObject firingPoint = new GameObject("FiringPoint");
                firingPoint.transform.SetParent(playerController.playerCamera.transform);
                firingPoint.transform.localPosition = Vector3.forward * 0.5f; // 0.5 metros adelante de la cámara
                firingPoint.transform.localRotation = Quaternion.identity;
                
                playerController.firingPoint = firingPoint;
                Debug.Log("FiringPoint creado automáticamente para " + playerInstance.name);
            }
            else
            {
                playerController.firingPoint = firingPointTransform.gameObject;
            }
        }
    }

    public void RespawnPlayer(NetworkObject playerNetworkObject)
    {
        if (!IsServer)
        {
            Debug.LogError("Solo el servidor puede hacer respawn!");
            return;
        }

        GameObject playerGameObject = playerNetworkObject.gameObject;

        Transform spawnPoint = SpawnManager.Instance.GetFarthestSpawnPoint(playerGameObject);

        if (spawnPoint != null)
        {
            playerGameObject.transform.position = spawnPoint.position;
            playerGameObject.transform.rotation = spawnPoint.rotation;

            Debug.Log($"Jugador {playerNetworkObject.OwnerClientId} respawneado en {spawnPoint.position}");
        }
    }
}