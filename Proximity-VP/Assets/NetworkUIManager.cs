using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class NetworkUIManager : MonoBehaviour
{
    [Header("UI Referencias")]
    public GameObject menuPanel;
    public Button hostButton;
    public Button clientButton;
    public TMP_InputField ipInputField;
    public TextMeshProUGUI statusText;

    [Header("Configuración")]
    public string defaultIP = "127.0.0.1";
    public ushort port = 7777;

    [Header("Escenas")]
    public string gameSceneName = "GameOnline";

    void Start()
    {
        if (hostButton != null)
            hostButton.onClick.AddListener(StartHost);

        if (clientButton != null)
            clientButton.onClick.AddListener(StartClient);

        if (ipInputField != null)
            ipInputField.text = defaultIP;

        UpdateStatus("Esperando conexión...");

        if (NetworkManager.Singleton != null)
        {
            // Por si quieres mostrar algo al conectar/desconectar
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
        }
    }

    void OnDestroy()
    {
        if (hostButton != null)
            hostButton.onClick.RemoveListener(StartHost);

        if (clientButton != null)
            clientButton.onClick.RemoveListener(StartClient);

        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
    }

    public void StartHost()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkManager no encontrado en la escena!");
            UpdateStatus("ERROR: NetworkManager no encontrado");
            return;
        }

        UpdateStatus("Iniciando Host...");

        SetupTransport();

        bool success = NetworkManager.Singleton.StartHost();

        if (success)
        {
            UpdateStatus($"Host iniciado en {GetLocalIP()}:{port}");
            HideMenu();

            // EL HOST CARGA LA ESCENA, LOS CLIENTES LA SEGUIRÁN
            NetworkManager.Singleton.SceneManager.LoadScene(
                gameSceneName, 
                LoadSceneMode.Single
            );
        }
        else
        {
            UpdateStatus("ERROR: No se pudo iniciar Host");
        }
    }

    public void StartClient()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkManager no encontrado en la escena!");
            UpdateStatus("ERROR: NetworkManager no encontrado");
            return;
        }

        UpdateStatus("Conectando...");

        SetupTransport();

        bool success = NetworkManager.Singleton.StartClient();

        if (success)
        {
            UpdateStatus($"Conectando a {GetTargetIP()}:{port}...");
            HideMenu();
            // el cliente solo espera; cuando el host cargue GameOnline,
            // Netcode lo llevará también a esa escena
        }
        else
        {
            UpdateStatus("ERROR: No se pudo conectar");
            ShowMenu();
        }
    }

    void SetupTransport()
    {
        var transport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();

        if (transport != null)
        {
            string targetIP = GetTargetIP();
            transport.ConnectionData.Address = targetIP;
            transport.ConnectionData.Port = port;

            Debug.Log($"Transporte configurado: {targetIP}:{port}");
        }
    }

    string GetTargetIP()
    {
        if (ipInputField != null && !string.IsNullOrEmpty(ipInputField.text))
            return ipInputField.text;

        return defaultIP;
    }

    string GetLocalIP()
    {
        try
        {
            var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    return ip.ToString();
            }
        }
        catch {}

        return "127.0.0.1";
    }

    void UpdateStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;

        Debug.Log($"[NetworkUI] {message}");
    }

    void HideMenu()
    {
        if (menuPanel != null)
            menuPanel.SetActive(false);
    }

    void ShowMenu()
    {
        if (menuPanel != null)
            menuPanel.SetActive(true);
    }

    void OnClientDisconnect(ulong clientId)
    {
        if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer)
        {
            // si somos cliente y nos echan, volvemos al menú de Matches
            UpdateStatus("Desconectado del servidor");
            ShowMenu();
        }
    }

    public void Disconnect()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
            UpdateStatus("Desconectado");
        }

        ShowMenu();
    }
}
