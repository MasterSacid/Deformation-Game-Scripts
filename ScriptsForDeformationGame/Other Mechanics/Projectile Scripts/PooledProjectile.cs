using UnityEngine;
using System.Collections.Generic;

public class PooledProjectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private float deformationForce = 40f;
    [SerializeField] private float lifeTime = 5f;
    [SerializeField] private GameObject impactEffectPrefab;
    [SerializeField] private LayerMask deformableLayers = -1;

    private float spawnTime;
    private Rigidbody rb;
    private bool hasHit = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void OnEnable()
    {
        spawnTime = Time.time;
        hasHit = false;
    }

    void Update()
    {
        // Auto-return to pool after lifetime
        if (Time.time - spawnTime > lifeTime)
        {
            ReturnToPool();
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return; // Prevent multiple hits

        // Check if we hit something deformable
        if ((deformableLayers.value & (1 << collision.gameObject.layer)) > 0)
        {
            MeshDeformer deformer = collision.gameObject.GetComponent<MeshDeformer>();
            if (deformer != null)
            {
                ContactPoint contact = collision.GetContact(0);
                deformer.AddDeformingForce(contact.point, deformationForce);

                // Create impact effect
                if (impactEffectPrefab != null)
                {
                    GameObject effect = Instantiate(impactEffectPrefab, contact.point,
                        Quaternion.LookRotation(contact.normal));
                    Destroy(effect, 2f);
                }

                hasHit = true;

                // Let the physics bounce happen, then return to pool after a delay
                Invoke(nameof(ReturnToPool), 0.5f);
            }
        }
        else
        {
           
        }
    }

    void ReturnToPool()
    {
        CancelInvoke(); // Cancel any pending returns
        ProjectilePool.Instance.ReturnProjectile(gameObject);
    }
}