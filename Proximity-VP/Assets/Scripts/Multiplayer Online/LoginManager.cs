using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using System.Collections;

public class LoginManager : MonoBehaviour
{
    public InputField usernameInput;
    public InputField passwordInput;
    public Text messageText;

    [Header("PHP URLs")]
    public string loginUrl    = "http://proximityvp.alwaysdata.net/screencheat/login.php";
    public string registerUrl = "http://proximityvp.alwaysdata.net/screencheat/register.php";

    private void Awake()
    {
        // Clave: que exista SIEMPRE antes de intentar hostear/join
        AccountSession.EnsureExists();
    }

    public void OnClickLogin()
    {
        StartCoroutine(LoginCoroutine());
    }

    public void OnClickRegister()
    {
        StartCoroutine(RegisterCoroutine());
    }

    IEnumerator LoginCoroutine()
    {
        messageText.text = "";

        WWWForm form = new WWWForm();
        form.AddField("username", usernameInput.text);
        form.AddField("password", passwordInput.text);

        using (UnityWebRequest www = UnityWebRequest.Post(loginUrl, form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                messageText.text = "Error de red";
                yield break;
            }

            var json = www.downloadHandler.text;
            LoginResponse resp = JsonUtility.FromJson<LoginResponse>(json);

            if (resp == null || !resp.success)
            {
                messageText.text = "Login incorrecto";
                yield break;
            }

            // ✅ Guardar sesión siempre
            AccountSession.EnsureExists().SetSession(resp.acc_id, resp.username);
            Debug.Log($"LOGIN OK -> acc_id={resp.acc_id} user={resp.username}");

            SceneManager.LoadScene("MatchesMenu");
        }
    }

    IEnumerator RegisterCoroutine()
    {
        messageText.text = "";

        WWWForm form = new WWWForm();
        form.AddField("username", usernameInput.text);
        form.AddField("password", passwordInput.text);

        using (UnityWebRequest www = UnityWebRequest.Post(registerUrl, form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                messageText.text = "Error de red";
                yield break;
            }

            messageText.text = "Cuenta creada, ahora loguea.";
        }
    }

    public void BackToMain()
    {
        SceneManager.LoadScene("MainMenu");
    }

    [System.Serializable]
    class LoginResponse
    {
        public bool success;
        public int acc_id;
        public string username;
        public string message;
    }
}
