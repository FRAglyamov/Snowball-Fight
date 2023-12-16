using UnityEngine;

public class BonusPickUp : MonoBehaviour
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
        if (collision.CompareTag("Player"))
        {
            collision.GetComponent<PlayerController>().BonusPickUp(bonusType, bonusTime, multiplier);
        }
    }
}
