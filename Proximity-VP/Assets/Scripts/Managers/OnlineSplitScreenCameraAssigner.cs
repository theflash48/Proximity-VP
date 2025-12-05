using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

/// <summary>
/// Asigna a cada Player (tag "Player") la RenderTexture que le corresponde
/// según su OwnerClientId.
/// - Index 0 del array -> ClientId 0
/// - Index 1 del array -> ClientId 1
/// etc.
/// </summary>
public class OnlineSplitScreenCameraAssigner : MonoBehaviour
{
    [Header("RenderTextures en orden de ClientId (0 = host, 1 = cliente 1, ...)")]
    [SerializeField] private RenderTexture[] renderTextures;
    
    private List<ulong> assignedClients = new List<ulong>();
    
    private void OnEnable()
    {
        if (NetworkManager.Singleton == null)
            return;

        // Nos enganchamos a los eventos
        NetworkManager.Singleton.OnClientConnectedCallback  += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
        
        // Asignación inicial inmediata
        StartCoroutine(InitialAssignment());
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton == null)
            return;

        NetworkManager.Singleton.OnClientConnectedCallback  -= OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Cliente {clientId} conectado. Asignando cámaras...");
        // Pequeño delay para asegurar que el objeto está spawnado
        Invoke(nameof(AssignAllCameras), 0.5f);
    }

    private void OnClientDisconnect(ulong clientId)
    {
        Debug.Log($"Cliente {clientId} desconectado. Limpiando asignación...");
        assignedClients.Remove(clientId);
    }

    private void OnSpawnedObjectsChanged()
    {
        // Cuando cambian los objetos spawnados, revisamos si hay nuevos jugadores
        AssignAllCameras();
    }

    private System.Collections.IEnumerator InitialAssignment()
    {
        // Espera un frame para que todo esté inicializado
        yield return null;
        
        // Asigna cámaras inmediatamente para el host y cualquier jugador existente
        AssignAllCameras();
        
        // Seguimos verificando periódicamente por si hay demoras en el spawn
        for (int i = 0; i < 3; i++)
        {
            yield return new WaitForSeconds(0.5f);
            AssignAllCameras();
        }
    }

    /// <summary>
    /// Busca todos los GOs con tag "Player", lee su OwnerClientId
    /// y asigna la RenderTexture correspondiente a su cámara.
    /// </summary>
    private void AssignAllCameras()
    {
        if (renderTextures == null || renderTextures.Length == 0)
        {
            Debug.LogWarning("[OnlineSplitScreenCameraAssigner] No hay RenderTextures asignadas.");
            return;
        }

        // Busca todos los jugadores en la escena
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        foreach (var player in players)
        {
            var netObj = player.GetComponent<NetworkObject>();
            if (netObj == null)
                continue;

            ulong clientId = netObj.OwnerClientId;
            
            // Si ya fue asignado, saltamos
            if (assignedClients.Contains(clientId))
                continue;

            // Intentamos usar la cámara que ya tienes en PlayerControllerOnline
            var controllerOnline = player.GetComponent<PlayerControllerOnline>();
            Camera cam = null;

            if (controllerOnline != null && controllerOnline.cameraComponent != null)
            {
                cam = controllerOnline.cameraComponent;
            }
            else
            {
                // Fallback: cualquier Camera hija del Player
                cam = player.GetComponentInChildren<Camera>(true);
            }

            if (cam == null)
            {
                Debug.LogWarning($"[OnlineSplitScreenCameraAssigner] Player {player.name} no tiene cámara.");
                continue;
            }

            // OwnerClientId es un ulong; lo usamos como índice del array
            int index = (int)clientId;

            if (index < 0 || index >= renderTextures.Length)
            {
                Debug.LogWarning(
                    $"[OnlineSplitScreenCameraAssigner] No hay RenderTexture para OwnerClientId {clientId}. " +
                    $"Añade más elementos al array (tamaño actual: {renderTextures.Length}).");
                continue;
            }

            cam.targetTexture = renderTextures[index];
            assignedClients.Add(clientId);

            // Opcional: log para depurar
            Debug.Log($"Asignada RT[{index}] a cámara de {player.name} (OwnerClientId={clientId}).");
        }
    }
}