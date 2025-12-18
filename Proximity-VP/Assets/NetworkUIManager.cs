using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using UnityEngine.UI;
using Unity.Netcode;
using UnityEngine;
using TMPro;

using Unity.Services.Core;
using Unity.Services.Authentication;

// Unified Multiplayer Services package (still implicitly used for authentication and Netcode setup)
using Unity.Services.Multiplayer;

// Explicitly use the standalone Lobby and Relay Services for their entry points as per migration guide
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

// Netcode and Transport
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay; // For RelayServerData (still used as a type)

public class NetworkUIManager : MonoBehaviour
{
    [Header("UI Referencias")]
    public GameObject menuPanel;
    public Button hostButton;
    public Button clientButton;
    public TMP_InputField ipInputField;
    public InputField inputJoinCode;
    public int joinCode;
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

        joinCode = StartHostWithRelay(4, "udp").GetHashCode();

        if (joinCode != 0)
        {
           UpdateStatus($"Host iniciado en {GetLocalIP()}:{port}");
           HideMenu();

           // EL HOST CARGA LA ESCENA, LOS CLIENTES LA SEGUIRÁN
           if (NetworkManager.Singleton != null)
           {
               NetworkManager.Singleton.StartHost();
               Debug.Log("NetworkManager si existe");
               NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
           }
        }
        else
        {
            UpdateStatus("ERROR: No se pudo iniciar Host");
        }
    }

    public async Task<string> StartHostWithRelay(int maxConnections, string connectionType)
    {
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        var allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, connectionType));
        var joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        return NetworkManager.Singleton.StartHost() ? joinCode : null;
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

        bool success = StartClientWithRelay(inputJoinCode.text, "udp").Result;

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

    public async Task<bool> StartClientWithRelay(string joinCode, string connectionType)
    {
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        var allocation = await RelayService.Instance.JoinAllocationAsync(joinCode: joinCode);
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, connectionType));
        return !string.IsNullOrEmpty(joinCode) && NetworkManager.Singleton.StartClient();
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
