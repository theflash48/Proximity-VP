using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInputManager))]
public class Lobby : MonoBehaviour
{
    PlayerInputManager inputManager;

    private void Awake()
    {
        inputManager = GetComponent<PlayerInputManager>();
    }

    public void OnPlayerJoined(PlayerInput input)
    {
        var player = input.gameObject;

        // Registrar al jugador en la lista
        SpawnManager.Instance.RegisterPlayer(player);

        // Obtener el mejor spawn (el mas lejano de los demas jugadores)
        var spawnPoint = SpawnManager.Instance.GetFarthestSpawnPoint(player);

        if (spawnPoint != null)
        {
            player.transform.position = spawnPoint.position;
            player.transform.rotation = spawnPoint.rotation;
        }
        else
        {
            Debug.LogWarning("No se encontro spawn point xd");
        }
    }

    public void OnPlayerLeft(PlayerInput input)
    {
        var player = input.gameObject;
        SpawnManager.Instance.UnregisterPlayer(player);
    }
}
