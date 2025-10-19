using UnityEngine;
using UnityEngine.EventSystems;

public class Shoot : MonoBehaviour
{
    // Ya no necesitamos spawnInicial ni instanciar la bala en Start
    // public GameObject spawnInicial;
    public GameObject bulletPrefab;
    public GameObject firingPoint;
    private GameObject bullet;
    private PlayerController pc;
    
    public int[] arrayInts;
    
    void Start()
    {
        pc = gameObject.GetComponent<PlayerController>();
        
        if (bulletPrefab != null)
        {
            bullet = Instantiate(bulletPrefab);
            bullet.SetActive(false); // La desactivamos hasta que se dispare
        }
    }

    public void ShootBullet()
    {
        if (firingPoint == null)
        {
            PlayerController playerController = GetComponent<PlayerController>();
            if (playerController != null && playerController.firingPoint != null)
            {
                firingPoint = playerController.firingPoint;
            }
            else
            {
                Debug.LogWarning("FiringPoint no encontrado en Shoot!");
                return;
            }
        }

        RaycastHit hit;
        if (Physics.Raycast(firingPoint.transform.position, firingPoint.transform.forward, out hit, 100f))
        {
            Debug.Log("Objeto golpeado: " + hit.transform.name + " en posición: " + hit.point);
            if (hit.transform.gameObject.CompareTag("Player"))
            {
                PlayerHealth pc_hit = hit.transform.gameObject.GetComponent<PlayerHealth>();
                
                if (pc_hit != null && pc_hit.GetCurrentLives() <= 0)
                {
                    pc.score++;
                }
            }
        }
        
        if (bullet != null)
        {
            bullet.SetActive(true);
            bullet.transform.position = firingPoint.transform.position;
            bullet.transform.rotation = firingPoint.transform.rotation;
            
            StartCoroutine(HideBulletAfterDelay(0.1f));
        }
    }

    System.Collections.IEnumerator HideBulletAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (bullet != null)
        {
            bullet.SetActive(false);
        }
    }
}