using UnityEngine;
using System.Collections.Generic;

// ===== OBJECT POOLING SOLUTION =====
// This is the most practical approach for physics projectiles
public class ProjectilePool : MonoBehaviour
{
    [Header("Pool Settings")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private int poolSize = 100;
    [SerializeField] private bool expandable = true;
    
    [Header("Optimization Settings")]
    [SerializeField] private bool useInstancingMaterial = true;
    [SerializeField] private int batchSize = 50; // Projectiles per batch
    
    private Queue<GameObject> pool = new Queue<GameObject>();
    private List<GameObject> activeProjectiles = new List<GameObject>();
    private MaterialPropertyBlock propertyBlock;
    private Material instancingMaterial;
    
    // Singleton for easy access
    private static ProjectilePool instance;
    public static ProjectilePool Instance => instance;
    
    void Awake()
    {
        instance = this;
        InitializePool();
        SetupInstancing();
    }
    
    void InitializePool()
    {
        // Pre-create all projectiles
        for (int i = 0; i < poolSize; i++)
        {
            CreateNewProjectile();
        }
    }
    
    void SetupInstancing()
    {
        if (useInstancingMaterial)
        {
            // Get the material and ensure it supports instancing
            MeshRenderer renderer = projectilePrefab.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                instancingMaterial = new Material(renderer.sharedMaterial);
                instancingMaterial.enableInstancing = true;
                propertyBlock = new MaterialPropertyBlock();
            }
        }
    }
    
    GameObject CreateNewProjectile()
    {
        GameObject proj = Instantiate(projectilePrefab);
        proj.SetActive(false);
        
        // Optimize the projectile
        OptimizeProjectile(proj);
        
        pool.Enqueue(proj);
        return proj;
    }
    
    void OptimizeProjectile(GameObject proj)
    {
        // Use instancing material
        if (instancingMaterial != null)
        {
            MeshRenderer renderer = proj.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = instancingMaterial;
                
                // Enable GPU instancing on renderer
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }
        
        // Optimize Rigidbody settings
        Rigidbody rb = proj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.None; // Saves performance
        }
        
        // Add pooled projectile component
        PooledProjectile pooledComp = proj.GetComponent<PooledProjectile>();
        if (pooledComp == null)
        {
            pooledComp = proj.AddComponent<PooledProjectile>();
        }
    }
    
    public GameObject GetProjectile()
    {
        GameObject proj;
        
        if (pool.Count > 0)
        {
            proj = pool.Dequeue();
        }
        else if (expandable)
        {
            proj = CreateNewProjectile();
            Debug.LogWarning($"Pool expanded! Consider increasing pool size. Active: {activeProjectiles.Count}");
        }
        else
        {
            Debug.LogError("Projectile pool exhausted!");
            return null;
        }
        
        proj.SetActive(true);
        activeProjectiles.Add(proj);
        return proj;
    }
    
    public void ReturnProjectile(GameObject proj)
    {
        if (proj == null) return;
        
        // Reset projectile state
        proj.SetActive(false);
        proj.transform.position = Vector3.zero;
        proj.transform.rotation = Quaternion.identity;
        
        // Reset physics
        Rigidbody rb = proj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        // Reset trail if exists
        TrailRenderer trail = proj.GetComponent<TrailRenderer>();
        if (trail != null) trail.Clear();
        
        activeProjectiles.Remove(proj);
        pool.Enqueue(proj);
    }
    
    // Call this periodically to clean up leaked projectiles
    public void CleanupInactiveProjectiles()
    {
        for (int i = activeProjectiles.Count - 1; i >= 0; i--)
        {
            if (activeProjectiles[i] == null || !activeProjectiles[i].activeInHierarchy)
            {
                activeProjectiles.RemoveAt(i);
            }
        }
    }
    
    void OnDestroy()
    {
        if (instancingMaterial != null)
        {
            Destroy(instancingMaterial);
        }
    }
}