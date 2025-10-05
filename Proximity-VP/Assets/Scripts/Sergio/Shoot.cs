using UnityEngine;
using UnityEngine.EventSystems;

public class Shoot : MonoBehaviour
{
    public GameObject spawnInicial;
    public GameObject bulletPrefab;
    public GameObject firingPoint;
    private GameObject bullet;
    
    public int[] arrayInts;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        spawnInicial = GameObject.Find("SpawnInicial");
        bullet = Instantiate (bulletPrefab, spawnInicial.transform.position, spawnInicial.transform.rotation);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ShootBullet()
    {
        bullet.transform.position = firingPoint.transform.position;
        bullet.transform.rotation = firingPoint.transform.rotation;
    }


}
