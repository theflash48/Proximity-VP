using UnityEngine;
using UnityEngine.EventSystems;

public class Shoot : MonoBehaviour
{
    public GameObject spawnInicial;
    public GameObject bulletPrefab;
    public GameObject firingPoint;
    private GameObject bullet;
    private PlayerController_PlayerInput pc;
    
    public int[] arrayInts;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        spawnInicial = GameObject.Find("SpawnInicial");
        pc = gameObject.GetComponent<PlayerController_PlayerInput>();
        bullet = Instantiate (bulletPrefab, spawnInicial.transform.position, spawnInicial.transform.rotation);
    }

    public void ShootBullet()
    {
        RaycastHit hit;
        if (Physics.Raycast(firingPoint.transform.position, firingPoint.transform.forward, out hit, 100f))
        {
            Debug.Log("Objeto golpeado: " + hit.transform.name + " en posición: " + hit.point);
            if (hit.transform.gameObject.CompareTag("Player"))
            {
                PlayerHealth pc_hit = hit.transform.gameObject.GetComponent<PlayerHealth>();
                if (pc_hit.currentLives <= 0)
                {
                    pc.score++;
                }
            }
        }
        bullet.transform.position = firingPoint.transform.position;
        bullet.transform.rotation = firingPoint.transform.rotation;
    }


}
