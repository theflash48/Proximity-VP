using System;
using Unity.VisualScripting;
using UnityEngine;

public class BulletScript : MonoBehaviour
{
    public float speed;
    
    [HideInInspector]
    public GameObject owner; // El jugador que disparó esta bala
    
    Rigidbody rb;
    
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        Destroy(gameObject, 3);
    }
    
    void FixedUpdate()
    {
        rb.MovePosition(rb.position + transform.forward * speed * Time.fixedDeltaTime);
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // No destruir si choca con el dueño de la bala
        if (other.gameObject == owner)
        {
            return;
        }
        
        // Destruir bala al chocar con cualquier cosa que no sea el player que la disparó
        if (!other.gameObject.CompareTag("Player"))
        {
            Destroy(gameObject);
        }
        else
        {
            // Si choca con otro jugador, también se destruye
            Destroy(gameObject);
        }
    }
}