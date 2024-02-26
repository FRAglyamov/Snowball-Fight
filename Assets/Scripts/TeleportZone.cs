using Unity.Netcode;
using UnityEngine;

public class TeleportZone : NetworkBehaviour
{
    [SerializeField]
    private Transform exitPoint;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (collision.GetComponent<NetworkObject>().IsOwner)
            {
                collision.transform.position = exitPoint.position;
                
            }
            if (IsServer)
            {
                //collision.GetComponent<PlayerController>().TeleportClientRpc(exitPoint.position);
            }
        }
    }
}
