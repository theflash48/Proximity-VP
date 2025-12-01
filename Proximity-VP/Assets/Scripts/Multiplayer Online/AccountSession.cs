using UnityEngine;

public class AccountSession : MonoBehaviour
{
    public static AccountSession Instance;

    public int AccId { get; private set; }
    public string Username { get; private set; }

    private void Awake()
    {
        if (Instance != null)
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

    public bool IsLoggedIn => AccId > 0;
}
