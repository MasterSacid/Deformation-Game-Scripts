using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class MeshDeformer : MonoBehaviour
{
    Mesh deformingMesh;
    Vector3[] originalVertices, displacedVertices;
    public Vector3[] vertexVelocities;
    
    [Header("Spring Settings")]
    public float springForce = 20f;
    public float damping = 5f;
    
    [Header("Deformation Settings")]
    [Tooltip("How the force decreases with distance")]
    public FalloffMode falloffMode = FalloffMode.InverseSquare;
    
    [Tooltip("Minimum distance at which force starts to fall off")]
    public float falloffStartRadius = 0.5f;
    
    [Tooltip("Distance at which force reaches zero")]
    public float falloffEndRadius = 3f;
    
    [Tooltip("Sharpness of the falloff curve (higher = more focused)")]
    public float falloffPower = 2f;
    
    [Tooltip("Multiplier for the deformation force")]
    public float forceMultiplier = 1f;
    
    [Tooltip("Maximum distance a vertex can be displaced")]
    public float maxDisplacement = 2f;
    
    float uniformScale = 1f;
    
    // Flag VERY IMPORTANT VERY VERY IMPORTANT !!!!!!!!
    private bool isInitialized = false;
    //-------------------------------------
    
    public enum FalloffMode
    {
        InverseSquare,
        Linear,
        Exponential,
        Gaussian,
        Constant
    }
    
    void Start()
    {
    
        InitializeMesh();
    }
    
    // Public method to initialize or reinitialize the mesh data
    public void InitializeMesh()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.mesh == null)
        {

            //Debug.LogWarning("No mesh found");
            return;
        }
        
        deformingMesh = meshFilter.mesh;
        
      
        if (deformingMesh.vertices == null || deformingMesh.vertices.Length == 0)
        {
            //Debug.LogWarning("Mesh has no vertices!");
            return;
        }
        
        originalVertices = deformingMesh.vertices;
        displacedVertices = new Vector3[originalVertices.Length];
        
        for (int i = 0; i < originalVertices.Length; i++)
        {
            displacedVertices[i] = originalVertices[i];
        }
        
        vertexVelocities = new Vector3[originalVertices.Length];
        
        isInitialized = true;
        //Debug.Log($"MeshDeformer initialized with {originalVertices.Length} vertices");
    }
    
    void Update()
    {
        // Skip update if not initialized
        if (!isInitialized || originalVertices == null || originalVertices.Length == 0)
        {
            return;
        }
        
        uniformScale = transform.localScale.x;
        
        for (int i = 0; i < displacedVertices.Length; i++)
        {
            UpdateVertex(i);
        }
        
        deformingMesh.vertices = displacedVertices;
        deformingMesh.RecalculateNormals();
    }
    
    void UpdateVertex(int i)
    {
        Vector3 velocity = vertexVelocities[i];
        Vector3 displacement = displacedVertices[i] - originalVertices[i];
        
        
        if (displacement.magnitude > maxDisplacement)
        {
            displacement = displacement.normalized * maxDisplacement;
            displacedVertices[i] = originalVertices[i] + displacement;
        }
        
        displacement *= uniformScale;
        velocity -= displacement * springForce * Time.deltaTime;
        velocity *= 1f - damping * Time.deltaTime;
        vertexVelocities[i] = velocity;
        displacedVertices[i] += velocity * (Time.deltaTime / uniformScale);
    }
    
    public void AddDeformingForce(Vector3 point, float force)
    {
        
        if (!isInitialized)
        {
            Debug.LogWarning("MeshDeformer: Cannot apply force - mesh not initialized!");
            return;
        }
        
        Debug.DrawLine(Camera.main.transform.position, point);
        point = transform.InverseTransformPoint(point);
        
        for (int i = 0; i < displacedVertices.Length; i++)
        {
            AddForceToVertex(i, point, force);
        }
    }
    
    void AddForceToVertex(int i, Vector3 point, float force)
    {
        //Inward Deformation
        Vector3 vertexToPoint = point - displacedVertices[i];
        vertexToPoint *= uniformScale;
        
        float distance = vertexToPoint.magnitude;
        
        // Skip some vertices that the ray doesn't reach out (Radius)
        if (distance > falloffEndRadius)
            return;
        float attenuatedForce = CalculateFalloff(distance, force);
        attenuatedForce *= forceMultiplier;
        float velocity = attenuatedForce * Time.deltaTime;
        
        //Program was exploding If I didn't control division by 0 (Took me 4 hours :()
        if (distance > 0.001f)
        {
            vertexVelocities[i] += vertexToPoint.normalized * velocity;
        }
    }
    
    float CalculateFalloff(float distance, float baseForce)
    {
        // Normalize distance
        float normalizedDistance = Mathf.Clamp01(
            (distance - falloffStartRadius) / (falloffEndRadius - falloffStartRadius)
        );
        
        float falloff = 1f;
        
        //---------------FALLOFF MODES-------------
        switch (falloffMode)
        {
            case FalloffMode.InverseSquare:
                if (distance < falloffStartRadius)
                    falloff = 1f;
                else
                    falloff = 1f / (1f + Mathf.Pow(distance - falloffStartRadius, 2));
                break;

            case FalloffMode.Linear:
                falloff = 1f - normalizedDistance;
                break;

            case FalloffMode.Exponential:
                falloff = Mathf.Pow(1f - normalizedDistance, falloffPower);
                break;
            //This one is my favourite
            case FalloffMode.Gaussian:
                float sigma = 0.4f; 
                falloff = Mathf.Exp(-0.5f * Mathf.Pow(normalizedDistance / sigma, 2));
                break;

            case FalloffMode.Constant:
                falloff = distance <= falloffEndRadius ? 1f : 0f;
                break;
        }
        
        return baseForce * falloff;
    }
    
    //-------GPT AREA-------
    // Method to create a shockwave effect - multiple impacts in a pattern (GPT is great!)
    public void AddShockwaveForce(Vector3 center, float force, int waveCount = 3)
    {
        for (int wave = 0; wave < waveCount; wave++)
        {
            float delay = wave * 0.1f; // Stagger the waves
            float waveForce = force * (1f - wave * 0.3f); // Each wave is weaker

            // You could use a coroutine here for actual delays
            AddDeformingForce(center, waveForce);
        }
    }
    
    // Method to create a directional deformation 
    public void AddDirectionalForce(Vector3 point, Vector3 direction, float force, float length)
    {
        int samples = Mathf.Max(5, (int)(length * 2));
        
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)(samples - 1);
            Vector3 samplePoint = point + direction * length * (t - 0.5f);
            
            // Force is stronger in the middle of the line
            float sampleForce = force * Mathf.Sin(t * Mathf.PI);
            AddDeformingForce(samplePoint, sampleForce);
        }
    }
    //---------GPT AREA ENDED-----------
    
    //Make the vertices go to their original place
    public void ResetDeformation()
    {
        if (!isInitialized) return;

        for (int i = 0; i < originalVertices.Length; i++)
        {
            displacedVertices[i] = originalVertices[i];
            vertexVelocities[i] = Vector3.zero;
        }

        deformingMesh.vertices = displacedVertices;
        deformingMesh.RecalculateNormals();
    }

    //DEBUGGING
    /*
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;
        
        // Draw the falloff radii
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, falloffStartRadius);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, falloffEndRadius);
        
        // Show initialization status
        if (!isInitialized)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
        }
    }
    */
}



public static class DeformationPatterns
{
    
    public static void CreateCrater(MeshDeformer deformer, Vector3 impactPoint, float force)
    {
        //push inward
        deformer.AddDeformingForce(impactPoint, force);
        int ringPoints = 8;
        float ringRadius = deformer.falloffStartRadius * 1.5f;
        
        for (int i = 0; i < ringPoints; i++)
        {
            float angle = (i / (float)ringPoints) * Mathf.PI * 2;
            Vector3 ringPoint = impactPoint + new Vector3(
                Mathf.Cos(angle) * ringRadius,
                0,
                Mathf.Sin(angle) * ringRadius
            );
            deformer.AddDeformingForce(ringPoint, -force * 0.3f);
        }
    }
    public static void CreateRipple(MeshDeformer deformer, Vector3 center, float force, int rippleCount)
    {
        for (int i = 0; i < rippleCount; i++)
        {
            float radius = deformer.falloffStartRadius + (i * deformer.falloffEndRadius / rippleCount);
            int pointCount = 12 + i * 4; 
            
            for (int j = 0; j < pointCount; j++)
            {
                float angle = (j / (float)pointCount) * Mathf.PI * 2;
                Vector3 point = center + new Vector3(
                    Mathf.Cos(angle) * radius,
                    0,
                    Mathf.Sin(angle) * radius
                );
                
            
                float rippleForce = force * (i % 2 == 0 ? 1f : -0.5f) / (i + 1);
                deformer.AddDeformingForce(point, rippleForce);
            }
        }
    }
}