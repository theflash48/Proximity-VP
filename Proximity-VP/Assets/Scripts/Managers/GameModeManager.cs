using UnityEngine;

public class GameModeManager : MonoBehaviour
{
    public enum conectionType
    {
        none,
        local,
        online
    }
    public conectionType conection;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        DontDestroyOnLoad(this.gameObject);
    }

}
