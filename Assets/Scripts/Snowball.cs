using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class Snowball : MonoBehaviour
{
    [SerializeField]
    private GameObject snowballImpactVFX;

    private void Start()
    {
        Destroy(gameObject, 5f);
    }

    //private void OnCollisionEnter2D(Collision2D collision)
    //{
    //    Destroy(gameObject);
    //}

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        Instantiate(snowballImpactVFX, transform.position, transform.rotation);
        // Create VFX and sound
    }
}
