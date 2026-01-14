using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInputManager))]
public class Lobby : MonoBehaviour
{
    private PlayerInputManager inputManager;

    private void Awake()
    {
        inputManager = GetComponent<PlayerInputManager>();
        ConfigureSplitScreen();
    }

    public void OnPlayerJoined(PlayerInput input)
    {
        var player = input.gameObject;

        SpawnManager.Instance.RegisterPlayer(player);

        var spawnPoint = SpawnManager.Instance.GetFarthestSpawnPoint(player);
        if (spawnPoint != null)
        {
            player.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
        }

        ConfigureSplitScreen();
    }

    public void OnPlayerLeft(PlayerInput input)
    {
        SpawnManager.Instance.UnregisterPlayer(input.gameObject);
        ConfigureSplitScreen();
    }

    private void ConfigureSplitScreen()
    {
        PlayerInput[] players = FindObjectsByType<PlayerInput>(FindObjectsSortMode.None);

        if (players.Length == 1) SetSinglePlayerViewport(players);
        else if (players.Length == 2) SetTwoPlayersViewport(players);
        else if (players.Length == 3) SetThreePlayersViewport(players);
        else if (players.Length >= 4) SetFourPlayersViewport(players);
    }

    private static Camera GetPlayerCamera(PlayerInput player)
    {
        if (player == null) return null;
        if (player.camera != null) return player.camera;

        var cam = player.GetComponentInChildren<Camera>(true);
        player.camera = cam;
        return cam;
    }

    private void SetSinglePlayerViewport(PlayerInput[] players)
    {
        foreach (var p in players)
        {
            var cam = GetPlayerCamera(p);
            if (cam == null) continue;
            cam.rect = new Rect(0f, 0f, 1f, 1f);
        }
    }

    private void SetTwoPlayersViewport(PlayerInput[] players)
    {
        float aspectScreen = (float)Screen.width / Screen.height;
        float height = (0.5f * aspectScreen) * (9f / 16f);
        if (height > 1f) height = 1f;
        float y = (1f - height) / 2f;

        foreach (var p in players)
        {
            var cam = GetPlayerCamera(p);
            if (cam == null) continue;

            if (p.playerIndex == 0) cam.rect = new Rect(0f, y, 0.5f, height);
            else if (p.playerIndex == 1) cam.rect = new Rect(0.5f, y, 0.5f, height);
        }
    }

    private void SetThreePlayersViewport(PlayerInput[] players)
    {
        foreach (var p in players)
        {
            var cam = GetPlayerCamera(p);
            if (cam == null) continue;

            if (p.playerIndex == 0) cam.rect = new Rect(0f, 0.5f, 0.5f, 0.5f);
            else if (p.playerIndex == 1) cam.rect = new Rect(0.5f, 0.5f, 0.5f, 0.5f);
            else cam.rect = new Rect(0.25f, 0f, 0.5f, 0.5f);
        }
    }

    private void SetFourPlayersViewport(PlayerInput[] players)
    {
        foreach (var p in players)
        {
            var cam = GetPlayerCamera(p);
            if (cam == null) continue;

            switch (p.playerIndex)
            {
                case 0: cam.rect = new Rect(0f, 0.5f, 0.5f, 0.5f); break;
                case 1: cam.rect = new Rect(0.5f, 0.5f, 0.5f, 0.5f); break;
                case 2: cam.rect = new Rect(0f, 0f, 0.5f, 0.5f); break;
                case 3: cam.rect = new Rect(0.5f, 0f, 0.5f, 0.5f); break;
                default: cam.rect = new Rect(0f, 0f, 1f, 1f); break;
            }
        }
    }
}
