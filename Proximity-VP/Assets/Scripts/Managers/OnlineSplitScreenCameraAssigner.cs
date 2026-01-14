using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class OnlineSplitScreenCameraAssigner : MonoBehaviour
{
    [Header("RenderTextures por SlotIndex (0..3)")]
    [SerializeField] private RenderTexture[] renderTextures;

    public void AssignAllCameras()
    {
        var players = GameObject.FindGameObjectsWithTag("Player");
        if (players == null || players.Length == 0) return;

        HashSet<int> assignedSlots = new HashSet<int>();

        foreach (var player in players)
        {
            if (player == null) continue;

            var cam = player.GetComponentInChildren<Camera>(true);
            if (cam == null) continue;

            int slot = -1;

            var id = player.GetComponent<PlayerIdentityOnline>();
            if (id != null && id.SlotIndex.Value >= 0)
                slot = id.SlotIndex.Value;
            else
            {
                // fallback por si todavía no se ha enviado identity
                var netObj = player.GetComponent<NetworkObject>();
                if (netObj != null) slot = (int)netObj.OwnerClientId;
            }

            if (slot < 0 || slot >= renderTextures.Length)
                continue;

            cam.targetTexture = renderTextures[slot];
            assignedSlots.Add(slot);
        }
    }

    void Start()
    {
        AssignAllCameras();
    }
}