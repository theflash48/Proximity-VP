using UnityEngine;
using UnityEngine.EventSystems;

public class Shoot : MonoBehaviour
{
    public GameObject firingPoint;
    
    public AudioSource audioSource;
    public AudioClip shootSound;

    // true si este objeto es un Player online
    private bool isOnline;

    void Start()
    {
        isOnline = GetComponent<PlayerControllerOnline>() != null;
    }

    /// <summary>
    /// Devuelve true si ha golpeado a un jugador (para sumar kill).
    /// </summary>
    public bool ShootBullet(GameObject playerCamera)
    {
        if (audioSource != null && shootSound != null)
            audioSource.PlayOneShot(shootSound);
        
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return false;

        if (firingPoint == null)
        {
            Debug.LogWarning("Shoot: firingPoint no asignado.");
            return false;
        }

        RaycastHit hit;
        if (Physics.Raycast(firingPoint.transform.position,
                            playerCamera.transform.forward,
                            out hit, 100f))
        {
            Debug.Log("Objeto golpeado: " + hit.transform.name + " en posición: " + hit.point);

            if (!hit.transform.CompareTag("Player"))
                return false;

            if (!isOnline)
            {
                // COUCH PARTY
                var phLocal = hit.transform.GetComponent<PlayerHealthLocal>();
                if (phLocal != null)
                {
                    phLocal.TakeDamage();
                    if (phLocal.currentLives <= 0)
                        return true; // kill
                }
            }
            else
            {
                // ONLINE
                var phOnline = hit.transform.GetComponent<PlayerHealthOnline>();
                if (phOnline != null)
                {
                    phOnline.TakeDamageServerRpc(); // el servidor aplica el daño
                    return true;                    // contamos hit para el score
                }
            }
        }

        return false;
    }
}
