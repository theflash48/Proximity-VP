using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkDisconnectHandler : MonoBehaviour
{
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private bool _serverForcedEndOnce;

    private void OnEnable()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (NetworkManager.Singleton == null) return;

        // ✅ Cliente: si me desconectan a mí, vuelvo al menú
        if (!NetworkManager.Singleton.IsServer && clientId == NetworkManager.Singleton.LocalClientId)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SceneManager.LoadScene(mainMenuSceneName);
            return;
        }

        // ✅ Servidor: si la partida ya empezó y ahora queda 1 cliente conectado,
        // forzamos fin de partida (la lógica de ganador final la cerraremos cuando metamos el manager de match).
        if (!NetworkManager.Singleton.IsServer) return;

        if (_serverForcedEndOnce) return;

        var timer = FindFirstObjectByType<TimerOnline>();
        if (timer == null) return;

        // Solo si ya había empezado la partida
        if (!timer.gameStarted) return;

        // Conectar “quedan 1” = ConnectedClientsIds incluye host también.
        // Con max 4, cuando solo queda uno conectado (incluye host), el count será 1.
        int connected = NetworkManager.Singleton.ConnectedClientsIds != null
            ? NetworkManager.Singleton.ConnectedClientsIds.Count
            : 0;

        if (connected <= 1)
        {
            _serverForcedEndOnce = true;
            timer.ForceEndGameServer();
        }
    }
}