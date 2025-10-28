using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEditor.Experimental.GraphView.GraphView;

[RequireComponent(typeof(PlayerInputManager))]
public class Lobby : MonoBehaviour
{
    PlayerInputManager inputManager;
    int currentPlayerCount = 0;

    private void Awake()
    {
        inputManager = GetComponent<PlayerInputManager>();

        ConfigureSplitScreen();
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

        currentPlayerCount++;
        ConfigureSplitScreen();
    }

    public void OnPlayerLeft(PlayerInput input)
    {
        var player = input.gameObject;
        SpawnManager.Instance.UnregisterPlayer(player);

        currentPlayerCount--;
        ConfigureSplitScreen();
    }

    void ConfigureSplitScreen()
    {
        PlayerInput[] players = FindObjectsOfType<PlayerInput>();

        switch (players.Length)
        {
            case 1:
                SetSinglePlayerViewport(players);
                break;
            case 2:
                SetTwoPlayersViewport(players);
                break;
            case 3:
                SetThreePlayersViewport(players);
                break;
            case 4:
                SetFourPlayersViewport(players);
                break;
        }
    }

    void SetSinglePlayerViewport(PlayerInput[] players)
    {
        foreach (PlayerInput player in players)
        {
            if (player.camera != null)
            {
                // Viewport completo para un solo jugador
                player.camera.rect = new Rect(0f, 0f, 1f, 1f);
            }
        }
    }

    void SetTwoPlayersViewport(PlayerInput[] players)
    {
        // Calcular la relación de aspecto actual de la pantalla
        float aspectScreen = (float)Screen.width / Screen.height;
        // Altura normalizada para cada viewport para mantener 16:9
        float height = (0.5f * aspectScreen) * (9f / 16f);

        // Si height es mayor que 1, significa que no cabe, pero en teoría para pantallas 16:9, height=0.5, que es menor que 1.
        // Si height es mayor que 1, la limitamos a 1, pero en realidad no debería pasar en pantallas normales.
        if (height > 1f) height = 1f;

        // Calcular la posición Y para centrar verticalmente
        float y = (1f - height) / 2f;

        foreach (PlayerInput player in players)
        {
            if (player.camera != null)
            {
                if (player.playerIndex == 0)
                {
                    player.camera.rect = new Rect(0f, y, 0.5f, height);
                }
                else if (player.playerIndex == 1)
                {
                    player.camera.rect = new Rect(0.5f, y, 0.5f, height);
                }
            }
        }
    }

    void SetThreePlayersViewport(PlayerInput[] players)
    {
        foreach (PlayerInput player in players)
        {
            if (player.camera != null)
            {
                // Para 3 jugadores: viewport completo
                player.camera.rect = new Rect(0f, 0f, 1f, 1f);
            }
        }
    }

    void SetFourPlayersViewport(PlayerInput[] players)
    {
        foreach (PlayerInput player in players)
        {
            if (player.camera != null)
            {
                // Para 4 jugadores: viewport completo
                player.camera.rect = new Rect(0f, 0f, 1f, 1f);
            }
        }
    }
}
