using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class Snowball : MonoBehaviour
{
    [SerializeField]
    private GameObject snowballImpactVFX;
    public bool isServerObject = false;
    public ulong playerOriginId;

    private void Start()
    {
        //if (!IsServer) return;

        Destroy(gameObject, 5f);
    }

    //private void Update()
    //{
    //    if (!IsServer) return;
    //    transform.Translate(Vector3.right * speed.Value * Time.deltaTime);
    //}

    private void OnTriggerEnter2D(Collider2D collision)
    {
        //if(IsClient)
        //{
        //    gameObject.SetActive(false);
        //    //Destroy(gameObject);
        //}
        //if (!IsServer) return;

        if (collision.CompareTag("Player") && isServerObject)
        {
            collision.GetComponent<PlayerController>().GetDamage(playerOriginId);
        }
        Destroy(gameObject);
        //UpdateSnowballPositionClientRpc(transform.position);
        //GetComponent<NetworkObject>().Despawn();
    }

    private void OnDestroy()
    {
        Instantiate(snowballImpactVFX, transform.position, transform.rotation);
    }
}
