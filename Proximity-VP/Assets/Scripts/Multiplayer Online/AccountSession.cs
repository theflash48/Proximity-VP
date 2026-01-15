using UnityEngine;

public class AccountSession : MonoBehaviour
{
    public static AccountSession Instance;

    public int AccId { get; private set; }
    public string Username { get; private set; }

    // ✅ Crea una instancia siempre, aunque nadie haya puesto el componente en una escena
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureExists();
    }

    public static AccountSession EnsureExists()
    {
        // Unity: Instance puede “parecer” null si el objeto fue destruido
        if (Instance != null)
            return Instance;

        var go = new GameObject("AccountSession");
        Instance = go.AddComponent<AccountSession>();
        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetSession(int accId, string username)
    {
        AccId = accId;
        Username = username;
    }

    public void ClearSession()
    {
        AccId = 0;
        Username = "";
    }

    public bool IsLoggedIn => AccId > 0;
}