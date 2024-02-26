using Unity.Netcode;
using UnityEngine;

public class BonusPickUp : NetworkBehaviour
{
    // Recommended values of multiplier:
    // Run, Throw Size - 1.5, Throw Force - 3, Throw Speed - 2, 0 - for bonuses, which not using it

    [SerializeField]
    private BonusType bonusType;
    [SerializeField]
    private float bonusTime;
    [Tooltip("Optional parameter")]
    [SerializeField]
    private float multiplier;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServer) return;

        if (collision.CompareTag("Player"))
        {
            collision.GetComponent<PlayerController>().BonusPickUpClientRpc(bonusType, bonusTime, multiplier);
        }
        gameObject.GetComponent<NetworkObject>().Despawn();
    }
}
