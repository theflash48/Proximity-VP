using UnityEngine;
using Unity.Netcode;

public class NetworkInitializer : MonoBehaviour
{
    public OnlineSplitScreenCameraAssigner cameraAssigner;
    
    void Start()
    {
        if (NetworkManager.Singleton != null)
        {
            // Si somos el host, forzar la asignación de cámaras inmediatamente
            if (NetworkManager.Singleton.IsServer)
            {
                Invoke(nameof(ForceCameraAssignment), 1f);
            }
        }
    }
    
    void ForceCameraAssignment()
    {
        if (cameraAssigner != null)
        {
            // Llamar al método público si lo creas
            // cameraAssigner.AssignAllCameras();
        }
        
        // También buscar y asignar manualmente
        var players = GameObject.FindGameObjectsWithTag("Player");
        foreach (var player in players)
        {
            var netObj = player.GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsOwner)
            {
                Debug.Log($"Forzando asignación para jugador OwnerClientId: {netObj.OwnerClientId}");
            }
        }
    }
}