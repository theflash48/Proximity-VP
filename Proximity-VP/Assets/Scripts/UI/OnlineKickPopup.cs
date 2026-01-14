using UnityEngine;
using UnityEngine.SceneManagement;

public class OnlineKickPopup : MonoBehaviour
{
    private static OnlineKickPopup _instance;
    private static bool _show;
    private static string _msg;

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
        _msg = message;
        _show = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
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
        {
            _show = false;

            // Intentar volver al menú principal si existe
            try { SceneManager.LoadScene("MainMenu"); } catch { /* si no existe, no pasa nada */ }
        }
    }
}