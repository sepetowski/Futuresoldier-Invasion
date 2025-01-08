using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletProjectal : MonoBehaviour
{
    [SerializeField] private float lifespan = 5f;

    public Vector3 direction;
    public float speed = 20f;
    public float damage;
    public float maxRange;

    private Vector3 startPosition;
    private Rigidbody bulletRigidbody;

    void Awake()
    {
        startPosition = transform.position;
        bulletRigidbody = GetComponent<Rigidbody>();

        bulletRigidbody.freezeRotation = true;
    }

    void Start()
    {
        // Set the bullet's initial velocity in the specified direction
        bulletRigidbody.velocity = direction.normalized * speed;

        Destroy(gameObject, lifespan);
    }

    private void Update()
    {
        if (Vector3.Distance(startPosition, transform.position) >= maxRange)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter(Collider other)
    {

        if (other.gameObject.layer == gameObject.layer)
            return;

        Physics.IgnoreCollision(other, GetComponent<Collider>());   
        Health targetHealth = other.GetComponent<Health>();

        if (targetHealth != null)
        {
            targetHealth.TakeDamage(damage);
            Debug.Log($"Bullet hit {other.gameObject.name}! Dealt {damage} damage.");
        }
        Destroy(gameObject);
    }
}
