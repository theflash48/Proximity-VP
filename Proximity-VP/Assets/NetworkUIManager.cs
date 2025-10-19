using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NetworkUIManager : MonoBehaviour
{
    [Header("UI Referencias")]
    [Tooltip("Panel que contiene los botones (se oculta al conectar)")]
    public GameObject menuPanel;
    
    [Tooltip("Botón para iniciar como Host (servidor + jugador)")]
    public Button hostButton;
    
    [Tooltip("Botón para iniciar como Cliente")]
    public Button clientButton;
    
    [Tooltip("Input field para la IP del servidor (opcional)")]
    public TMP_InputField ipInputField;
    
    [Tooltip("Texto para mostrar estado de conexión")]
    public TextMeshProUGUI statusText;

    [Header("Configuración")]
    [Tooltip("IP por defecto si no se especifica otra")]
    public string defaultIP = "127.0.0.1";
    
    [Tooltip("Puerto de conexión")]
    public ushort port = 7777;

    void Start()
    {
        if (hostButton != null)
        {
            hostButton.onClick.AddListener(StartHost);
        }

        if (clientButton != null)
        {
            clientButton.onClick.AddListener(StartClient);
        }

        if (ipInputField != null)
        {
            ipInputField.text = defaultIP;
        }

        UpdateStatus("Esperando conexión...");
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

        // Iniciar como host
        bool success = NetworkManager.Singleton.StartHost();

        if (success)
        {
            UpdateStatus($"Host iniciado en {GetLocalIP()}:{port}");
            HideMenu();
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

        // Iniciar como cliente
        bool success = NetworkManager.Singleton.StartClient();

        if (success)
        {
            UpdateStatus($"Conectando a {GetTargetIP()}:{port}...");
            HideMenu();
        }
        else
        {
            UpdateStatus("ERROR: No se pudo conectar");
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
        {
            return ipInputField.text;
        }
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
                {
                    return ip.ToString();
                }
            }
        }
        catch
        {
            return "127.0.0.1";
        }
        return "127.0.0.1";
    }

    void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        Debug.Log($"[NetworkUI] {message}");
    }

    void HideMenu()
    {
        if (menuPanel != null)
        {
            menuPanel.SetActive(false);
        }
    }

    void ShowMenu()
    {
        if (menuPanel != null)
        {
            menuPanel.SetActive(true);
        }
    }

    public void Disconnect()
    {
        if (NetworkManager.Singleton != null)
        {
            if (NetworkManager.Singleton.IsHost)
            {
                NetworkManager.Singleton.Shutdown();
                UpdateStatus("Host detenido");
            }
            else if (NetworkManager.Singleton.IsClient)
            {
                NetworkManager.Singleton.Shutdown();
                UpdateStatus("Desconectado del servidor");
            }
        }

        ShowMenu();
    }

    void OnDestroy()
    {
        // Limpiar listeners
        if (hostButton != null)
        {
            hostButton.onClick.RemoveListener(StartHost);
        }

        if (clientButton != null)
        {
            clientButton.onClick.RemoveListener(StartClient);
        }
    }
}