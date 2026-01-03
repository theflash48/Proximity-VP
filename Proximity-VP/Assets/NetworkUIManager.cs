using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using System.Collections;
using UnityEngine.UI;
using Unity.Netcode;
using UnityEngine;
using System;

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
    public InputField ipInput;
    public InputField inputJoinCode;
    public int joinCode;
    public Text statusMSG;

    [Header("Configuración")]
    public string defaultIP = "127.0.0.1";
    public ushort port = 7777;

    [Header("Escenas")]
    public string gameSceneName = "GameOnline";

    private bool isSigningIn = false;
    private bool servicesInitialized = false;

    void Start()
    {
        if (hostButton != null)
            hostButton.onClick.AddListener(BeginStartHost);

        if (clientButton != null)
            clientButton.onClick.AddListener(BeginStartClient);

        if (ipInput != null)
            ipInput.text = defaultIP;

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
            hostButton.onClick.RemoveListener(BeginStartHost);

        if (clientButton != null)
            clientButton.onClick.RemoveListener(BeginStartClient);

        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
    }

    public void BeginStartHost()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkManager no encontrado en la escena!");
            UpdateStatus("ERROR: NetworkManager no encontrado");
            return;
        }

        StartCoroutine(StartHost());
    }

    IEnumerator StartHost()
    {

        UpdateStatus("Iniciando Host...");

        SetupTransport();

        joinCode = StartHostWithRelay(4, "udp").GetHashCode();

        yield return new WaitForSeconds(5);

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
               StopCoroutine(StartHost());
           }
        }
        else
        {
            UpdateStatus("ERROR: No se pudo iniciar Host");
            StopCoroutine(StartHost());
        }
    }

    public async Task<string> StartHostWithRelay(int maxConnections, string connectionType)
    {
        if (!servicesInitialized)
        {
            await UnityServices.InitializeAsync();
            servicesInitialized = true;
        }

        if (!AuthenticationService.Instance.IsSignedIn && isSigningIn)
        {
            isSigningIn = true;
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            isSigningIn = false;
        }

        var allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, connectionType));
        Debug.LogError("Relay");
        var joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        return NetworkManager.Singleton.StartHost() ? joinCode : null;
    }

    public void BeginStartClient()
    {
        Debug.LogError("1");
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkManager no encontrado en la escena!");
            UpdateStatus("ERROR: NetworkManager no encontrado");
            return;
        }

        StartCoroutine(StartClient());
    }

    IEnumerator StartClient()
    {
        UpdateStatus("Conectando...");

        SetupTransport();

        var task = StartClientWithRelay(inputJoinCode.text, "udp");

        while (!task.IsCompleted)
            yield return null;

        bool success = task.Result;

        if (success)
        {
            UpdateStatus($"Conectando a {GetTargetIP()}:{port}...");
            HideMenu();
            StopCoroutine(StartClient());
            // el cliente solo espera; cuando el host cargue GameOnline,
            // Netcode lo llevará también a esa escena
        }
        else
        {

            UpdateStatus("ERROR: No se pudo conectar");
            ShowMenu();
            StopCoroutine(StartClient());
        }

    }

    public async Task<bool> StartClientWithRelay(string joinCode, string connectionType)
    {
        try
        {
            if (!servicesInitialized)
            {
                await UnityServices.InitializeAsync();
                servicesInitialized = true;
            }

            if (!AuthenticationService.Instance.IsSignedIn && !isSigningIn)
            {
                isSigningIn = true;
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                isSigningIn = false;
            }

            var allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.Shutdown();
                await Task.Yield();
            }

            await UnityThread.SwitchToMainThread();

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(
                AllocationUtils.ToRelayServerData(allocation, connectionType)
            );

            return NetworkManager.Singleton.StartClient();
        }
        catch (Exception e)
        {
            Debug.LogError($"StartClientWithRelay failed:\n{e}");
            return false;
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
        if (ipInput != null && !string.IsNullOrEmpty(ipInput.text))
            return ipInput.text;

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
        if (statusMSG != null)
            statusMSG.text = message;

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
