using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class OnlineKickPopup : MonoBehaviour
{
    private static OnlineKickPopup _instance;
    private static bool _show;
    private static string _msg;

    // Auto-salida
    private static float _autoCloseAt = -1f;
    private const float DefaultAutoCloseSeconds = 2.0f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (_instance != null) return;

        var go = new GameObject("OnlineKickPopup");
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<OnlineKickPopup>();
    }

    public static void Show(string message)
    {
        if (_instance == null) Boot();

        _msg = message;
        _show = true;

        // Autocierre para que el duplicado "se autosalga"
        _autoCloseAt = Time.unscaledTime + DefaultAutoCloseSeconds;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update()
    {
        if (!_show) return;

        if (_autoCloseAt > 0f && Time.unscaledTime >= _autoCloseAt)
            CloseAndReturnToMenu();
    }

    private static void CloseAndReturnToMenu()
    {
        _show = false;
        _autoCloseAt = -1f;

        // Cerrar red si sigue activa
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.Shutdown();

        // Volver al menú principal
        try { SceneManager.LoadScene("MainMenu"); } catch { /* si no existe, no pasa nada */ }
    }

    void OnGUI()
    {
        if (!_show) return;

        float w = 520f;
        float h = 170f;
        Rect r = new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h);

        GUI.ModalWindow(9999, r, DrawWindow, "Conexión");
    }

    void DrawWindow(int id)
    {
        GUILayout.Space(10);
        GUILayout.Label(_msg);
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("OK", GUILayout.Height(32)))
            CloseAndReturnToMenu();
    }
}
