using UnityEngine;
using UnityEngine.EventSystems;

public class Shoot : MonoBehaviour
{
    // Ya no necesitamos spawnInicial ni instanciar la bala en Start
    // public GameObject spawnInicial;
    public GameObject firingPoint;
    private PlayerControllerLocal pcl;
    private PlayerControllerOnline pco;
    private bool isOnline = false;
    
    void Start()
    {
        if (gameObject.GetComponent<PlayerControllerLocal>())
        {
            isOnline = false;
            pcl = gameObject.GetComponent<PlayerControllerLocal>();
        }
        if (gameObject.GetComponent<PlayerControllerOnline>())
        {
            isOnline = true;
            pco = gameObject.GetComponent<PlayerControllerOnline>();
        }
    }

    public bool ShootBullet(GameObject playerCamera)
    {
        if (firingPoint == null)
        {
            Debug.LogWarning("FiringPoint no encontrado en Shoot!");
            return false;
        }
            
        RaycastHit hit;
        if (Physics.Raycast(firingPoint.transform.position, playerCamera.transform.forward, out hit, 100f))
        {
            Debug.Log("Objeto golpeado: " + hit.transform.name + " en posición: " + hit.point);
            if (hit.transform.gameObject.CompareTag("Player"))
            {
                bool local = false;
                PlayerHealthLocal pc_hit = hit.transform.gameObject.GetComponent<PlayerHealthLocal>();
                
                if (pc_hit != null && pc_hit.currentLives <= 0)
                {
                    return true;
                }
            }
        }
        return false;
    }
}