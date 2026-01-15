using UnityEngine;

public class AccountSession : MonoBehaviour
{
    public static AccountSession Instance;

    public int AccId { get; private set; }
    public string Username { get; private set; }

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

    /// <summary>
    /// Garantiza que exista un AccountSession en runtime.
    /// Útil porque en tu proyecto a veces no está en ninguna jerarquía.
    /// </summary>
    public static AccountSession EnsureExists()
    {
        if (Instance != null) return Instance;

        var existing = FindFirstObjectByType<AccountSession>();
        if (existing != null)
        {
            Instance = existing;
            DontDestroyOnLoad(existing.gameObject);
            return Instance;
        }

        var go = new GameObject("AccountSession");
        Instance = go.AddComponent<AccountSession>();
        return Instance;
    }

    public void SetSession(int accId, string username)
    {
        AccId = accId;
        Username = username ?? "";
    }

    public void ClearSession()
    {
        AccId = 0;
        Username = "";
    }

    public bool IsLoggedIn => AccId > 0;
}