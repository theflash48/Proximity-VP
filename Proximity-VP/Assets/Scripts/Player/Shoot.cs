using UnityEngine;
using UnityEngine.EventSystems;
using Unity.Netcode;

public class Shoot : MonoBehaviour
{
    public GameObject firingPoint;

    public AudioSource audioSource;
    public AudioClip shootSound;

    private PlayerControllerOnline pcOnline;

    void Awake()
    {
        pcOnline = GetComponent<PlayerControllerOnline>();
    }

    private bool IsOnlineMode()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            return true;

        if (pcOnline != null && pcOnline.enabled)
            return true;

        return false;
    }

    public bool ShootBullet(GameObject playerCamera)
    {
        if (audioSource != null && shootSound != null)
            audioSource.PlayOneShot(shootSound);

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return false;

        if (firingPoint == null || playerCamera == null)
            return false;

        Vector3 origin = firingPoint.transform.position;
        Vector3 dir = playerCamera.transform.forward;

        if (!Physics.Raycast(origin, dir, out RaycastHit hit, 100f, ~0, QueryTriggerInteraction.Ignore))
            return false;

        if (hit.transform != null && hit.transform.root == transform.root)
            return false;

        if (IsOnlineMode())
        {
            var phOnline = hit.collider.GetComponentInParent<PlayerHealthOnline>();
            if (phOnline != null)
            {
                ulong shooterId = (NetworkManager.Singleton != null) ? NetworkManager.Singleton.LocalClientId : 0;
                phOnline.TakeDamageServerRpc(shooterId);
                return true;
            }
        }
        else
        {
            var phLocal = hit.collider.GetComponentInParent<PlayerHealthLocal>();
            if (phLocal != null)
            {
                phLocal.TakeDamage();
                return phLocal.currentLives <= 0;
            }
        }

        return false;
    }
}
