using UnityEngine;
using UnityEngine.InputSystem;

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
        // Calcular la relaci�n de aspecto actual de la pantalla
        float aspectScreen = (float)Screen.width / Screen.height;
        // Altura normalizada para cada viewport para mantener 16:9
        float height = (0.5f * aspectScreen) * (9f / 16f);

        // Si height es mayor que 1, significa que no cabe, pero en teor�a para pantallas 16:9, height=0.5, que es menor que 1.
        // Si height es mayor que 1, la limitamos a 1, pero en realidad no deber�a pasar en pantallas normales.
        if (height > 1f) height = 1f;

        // Calcular la posici�n Y para centrar verticalmente
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
            if (player.camera == null) continue;


            if (player.playerIndex == 0)
            {
                player.camera.rect = new Rect(0f, 0.5f, 0.5f, 0.5f);
            }
            else if (player.playerIndex == 1)
            {
                player.camera.rect = new Rect(0.5f, 0.5f, 0.5f, 0.5f);
            }
            else // playerIndex == 2 (o cualquier otro �ndice restante)
            {
                player.camera.rect = new Rect(0.25f, 0f, 0.5f, 0.5f);
            }
        }
    }

    void SetFourPlayersViewport(PlayerInput[] players)
    {
        foreach (PlayerInput player in players)
        {
            if (player.camera == null) continue;

            // Asignaci�n por playerIndex:
            // 0 -> arriba izquierda, 1 -> arriba derecha,
            // 2 -> abajo izquierda, 3 -> abajo derecha
            switch (player.playerIndex)
            {
                case 0:
                    player.camera.rect = new Rect(0f, 0.5f, 0.5f, 0.5f); // top-left
                    break;
                case 1:
                    player.camera.rect = new Rect(0.5f, 0.5f, 0.5f, 0.5f); // top-right
                    break;
                case 2:
                    player.camera.rect = new Rect(0f, 0f, 0.5f, 0.5f); // bottom-left
                    break;
                case 3:
                    player.camera.rect = new Rect(0.5f, 0f, 0.5f, 0.5f); // bottom-right
                    break;
                default:
                    // Fallback razonable por si los �ndices no son 0..3
                    player.camera.rect = new Rect(0f, 0f, 1f, 1f);
                    break;
            }
        }
    }
}
