using UnityEngine;
using System.Collections.Generic;

public class OptimizedProjectileShooter : MonoBehaviour
{
    [Header("Shooting Settings")]
    [SerializeField] private Transform shootPoint;
    
    // Made these public properties so UI can access them
    public float projectileSpeed = 30f;
    public float fireRate = 0.1f;
    public int burstCount = 1;
    public float burstSpread = 5f;
    
    private float nextFireTime = 0f;
    
    void Update()
    {
        if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
        {
            FireBurst();
            nextFireTime = Time.time + fireRate;
        }
        
        // Debug info
        if (Input.GetKeyDown(KeyCode.I))
        {
            ProjectilePool.Instance.CleanupInactiveProjectiles();
            Debug.Log("Pool cleaned up");
        }
    }
    
    void FireBurst()
    {
        for (int i = 0; i < burstCount; i++)
        {
            FireSingleProjectile(i);
        }
    }
    
    void FireSingleProjectile(int index)
    {
        GameObject projectile = ProjectilePool.Instance.GetProjectile();
        if (projectile == null) return;
        
        // Position and rotation with spread
        projectile.transform.position = shootPoint.position;
        
        // Calculate spread
        float spreadAngle = 0f;
        if (burstCount > 1)
        {
            float spreadStep = burstSpread / (burstCount - 1);
            spreadAngle = -burstSpread / 2 + (spreadStep * index);
        }
        
        Quaternion rotation = shootPoint.rotation * Quaternion.Euler(0, spreadAngle, 0);
        projectile.transform.rotation = rotation;
        
        // Apply velocity
        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = rotation * Vector3.forward * projectileSpeed;
        }
    }
}