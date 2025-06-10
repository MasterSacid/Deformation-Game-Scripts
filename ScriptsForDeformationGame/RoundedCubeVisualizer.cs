using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class RoundedCubeVisualizer : MonoBehaviour
{
    [Header("Cube Settings")]
    public int xSize = 4, ySize = 4, zSize = 4;
    public int roundness = 1;

    [Header("Visualization Settings")]
    public GameObject vertexSpherePrefab; 
    public float vertexCreationDelay = 0.05f; 
    public float triangleCreationDelay = 0.01f; 
    public bool showTriangleCreation = true; 

    [Header("Visual Feedback")]
    public Color vertexColorStart = Color.yellow;
    public Color vertexColorEnd = Color.green;
    public Material transparentMaterial; 

    [Header("Generation Mode")]
    public bool generateOnAwake = false;
    public bool animateGeneration = true;

    [Header("Initial State")]
    public bool startWithEmptyObject = true;

    private Mesh mesh;
    private Vector3[] vertices;
    private Vector3[] normals;
    private Color32[] cubeUV;

    private List<GameObject> vertexSpheres = new List<GameObject>();
    private bool isGenerating = false;

    void Awake()
    {
        if (generateOnAwake)
        {
            if (animateGeneration)
            {
                
            }
            else
            {
               
                GenerateInstant();
            }
        }
    }

    void Start()
    {
    
        if (!generateOnAwake && startWithEmptyObject)
        {
            DisableRendering();
        }
    }

    //----------------Generate the object immediatly without animation (for debug purposes)-----------
    public void GenerateInstant()
    {
        ClearVisualization();
        EnableRendering();

        // Initialize mesh
        GetComponent<MeshFilter>().mesh = mesh = new Mesh();
        mesh.name = "Procedural Cube";

   
        CreateVerticesInstant();
        CreateTriangles();
        CreateColliders();

        // Initialize MeshDeformer
        MeshDeformer deformer = GetComponent<MeshDeformer>();
        if (deformer != null)
        {
            deformer.InitializeMesh();
        }

        Debug.Log("Rounded cube generated instantly!");
    }

    private void CreateVerticesInstant()
    {
        int cornerVertices = 8;
        int edgeVertices = (xSize + ySize + zSize - 3) * 4;
        int faceVertices = (
            (xSize - 1) * (ySize - 1) +
            (xSize - 1) * (zSize - 1) +
            (ySize - 1) * (zSize - 1)) * 2;

        vertices = new Vector3[cornerVertices + edgeVertices + faceVertices];
        normals = new Vector3[vertices.Length];
        cubeUV = new Color32[vertices.Length];

        int v = 0;

       
        for (int y = 0; y <= ySize; y++)
        {
            
            for (int x = 0; x <= xSize; x++)
                SetVertexInstant(v++, x, y, 0);

            for (int z = 1; z <= zSize; z++)
                SetVertexInstant(v++, xSize, y, z);

            for (int x = xSize - 1; x >= 0; x--)
                SetVertexInstant(v++, x, y, zSize);

            for (int z = zSize - 1; z > 0; z--)
                SetVertexInstant(v++, 0, y, z);
        }

        // Top face 
        for (int z = 1; z < zSize; z++)
            for (int x = 1; x < xSize; x++)
                SetVertexInstant(v++, x, ySize, z);

        // Bottom face 
        for (int z = 1; z < zSize; z++)
            for (int x = 1; x < xSize; x++)
                SetVertexInstant(v++, x, 0, z);

        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.colors32 = cubeUV;
    }

    //------------- Set vertex without visualization-----------
    private void SetVertexInstant(int i, int x, int y, int z)
    {
        Vector3 centeredPosition = new Vector3(
            x - xSize * 0.5f,
            y - ySize * 0.5f,
            z - zSize * 0.5f
        );

        Vector3 inner = centeredPosition;

        float xRadius = xSize * 0.5f - roundness;
        float yRadius = ySize * 0.5f - roundness;
        float zRadius = zSize * 0.5f - roundness;

        if (inner.x < -xRadius)
            inner.x = -xRadius;
        else if (inner.x > xRadius)
            inner.x = xRadius;

        if (inner.y < -yRadius)
            inner.y = -yRadius;
        else if (inner.y > yRadius)
            inner.y = yRadius;

        if (inner.z < -zRadius)
            inner.z = -zRadius;
        else if (inner.z > zRadius)
            inner.z = zRadius;

        // Calculate normals
        normals[i] = (centeredPosition - inner).normalized;
        vertices[i] = inner + normals[i] * roundness;
        cubeUV[i] = new Color32((byte)x, (byte)y, (byte)z, 0);
    }


    public bool IsGenerating()
    {
        return isGenerating;
    }

    public IEnumerator GenerateWithVisualization()
    {
        isGenerating = true;

        Rigidbody rb = GetComponent<Rigidbody>();
        bool hadPhysicsEnabled = false;

       
        // Freeze physics during generation
        if (rb != null)
        {
            hadPhysicsEnabled = !rb.isKinematic;
            rb.isKinematic = true; 
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }


        ClearVisualization();
        EnableRendering();

        // Disabling MeshDeformer during generation to prevent initialization issues (I tried to disable before butcouldnt make it happen)
        MeshDeformer deformer = GetComponent<MeshDeformer>();
        if (deformer != null)
        {
            deformer.enabled = false;
        }

        // Initialize mesh
        GetComponent<MeshFilter>().mesh = mesh = new Mesh();
        mesh.name = "Procedural Cube (Visualized)";

        // Calculate total vertices
        int cornerVertices = 8;
        int edgeVertices = (xSize + ySize + zSize - 3) * 4;
        int faceVertices = (
            (xSize - 1) * (ySize - 1) +
            (xSize - 1) * (zSize - 1) +
            (ySize - 1) * (zSize - 1)) * 2;

        vertices = new Vector3[cornerVertices + edgeVertices + faceVertices];
        normals = new Vector3[vertices.Length];
        cubeUV = new Color32[vertices.Length];

        // Create vertices with visualization
        yield return StartCoroutine(CreateVerticesWithVisualization());

        // Assign vertices
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.colors32 = cubeUV;

        if (showTriangleCreation)
        {
            yield return StartCoroutine(CreateTrianglesWithVisualization());
        }
        else
        {
            CreateTriangles();
        }
        CreateColliders();
        yield return StartCoroutine(FinalizeVisualization());

        //Enable Mesh Deformer
        if (deformer != null)
        {
            deformer.enabled = true;
            deformer.InitializeMesh();
        }

        //Enable Physics again
        if (rb != null)
        {
            rb.isKinematic = !hadPhysicsEnabled;
        }


        isGenerating = false;
        Debug.Log("Rounded cube generation complete!");
    }

    private IEnumerator CreateVerticesWithVisualization()
    {
        int v = 0;
        for (int y = 0; y <= ySize; y++)
        {
            //Debug.Log($"Creating vertices for Y level {y}");

            // Front edge
            for (int x = 0; x <= xSize; x++)
            {
                yield return StartCoroutine(SetVertexWithVisualization(v++, x, y, 0));
            }

            // Right edge
            for (int z = 1; z <= zSize; z++)
            {
                yield return StartCoroutine(SetVertexWithVisualization(v++, xSize, y, z));
            }

            // Back edge
            for (int x = xSize - 1; x >= 0; x--)
            {
                yield return StartCoroutine(SetVertexWithVisualization(v++, x, y, zSize));
            }

            // Left edge
            for (int z = zSize - 1; z > 0; z--)
            {
                yield return StartCoroutine(SetVertexWithVisualization(v++, 0, y, z));
            }

            yield return new WaitForSeconds(vertexCreationDelay * 2);
        }

        // -------------- Inner Faces -------------
        // Top face
        //Debug.Log("Top verticess started");
        //Debug.Log("Creating top face vertices");
        for (int z = 1; z < zSize; z++)
        {
            for (int x = 1; x < xSize; x++)
            {
                yield return StartCoroutine(SetVertexWithVisualization(v++, x, ySize, z));
            }
        }

        // Bottom face
        //Debug.Log("Creating bottom face vertices");
        for (int z = 1; z < zSize; z++)
        {
            for (int x = 1; x < xSize; x++)
            {
                yield return StartCoroutine(SetVertexWithVisualization(v++, x, 0, z));
            }
        }
    }

    private IEnumerator SetVertexWithVisualization(int i, int x, int y, int z)
    {
        Vector3 centeredPosition = new Vector3(
            x - xSize * 0.5f,
            y - ySize * 0.5f,
            z - zSize * 0.5f
        );

        Vector3 inner = centeredPosition;

        float xRadius = xSize * 0.5f - roundness;
        float yRadius = ySize * 0.5f - roundness;
        float zRadius = zSize * 0.5f - roundness;

        if (inner.x < -xRadius)
            inner.x = -xRadius;
        else if (inner.x > xRadius)
            inner.x = xRadius;

        if (inner.y < -yRadius)
            inner.y = -yRadius;
        else if (inner.y > yRadius)
            inner.y = yRadius;

        if (inner.z < -zRadius)
            inner.z = -zRadius;
        else if (inner.z > zRadius)
            inner.z = zRadius;

        // Calculate normals
        normals[i] = (centeredPosition - inner).normalized;
        vertices[i] = inner + normals[i] * roundness;
        cubeUV[i] = new Color32((byte)x, (byte)y, (byte)z, 0);

        if (vertexSpherePrefab != null)
        {
            GameObject sphere = Instantiate(vertexSpherePrefab, transform);
            sphere.transform.localPosition = vertices[i];
            sphere.name = $"Vertex_{i} ({x},{y},{z})";

            sphere.transform.localScale = Vector3.one * 0.1f;

            Renderer renderer = sphere.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = vertexColorStart;
            }

            vertexSpheres.Add(sphere);
        }
        yield return new WaitForSeconds(vertexCreationDelay);
    }


    //-----------Triangle creation (Visaulizaing)----------
    //Small note: This triangle visualization doesnt work since I am using a custom shader for cube and sphere. Unluky I guess (It would work without assigning a material and a custom shader)
    private IEnumerator CreateTrianglesWithVisualization()
    {
       //Debug.Log("Creating triangles...");

        CreateTriangles();

        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (transparentMaterial != null)
        {
            Material originalMat = mr.material;
            mr.material = transparentMaterial;
            yield return new WaitForSeconds(0.5f);
            mr.material = originalMat;
        }

        yield return null;
    }

    private IEnumerator FinalizeVisualization()
    {
        //change sphere colors upon complete
        float duration = 1f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            Color currentColor = Color.Lerp(vertexColorStart, vertexColorEnd, t);

            foreach (GameObject sphere in vertexSpheres)
            {
                if (sphere != null)
                {
                    Renderer renderer = sphere.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.material.color = currentColor;
                    }
                }
            }

            yield return null;
        }
    }

    private void ClearVisualization()
    {
        foreach (GameObject sphere in vertexSpheres)
        {
            if (sphere != null)
                DestroyImmediate(sphere);
        }
        vertexSpheres.Clear();

        if (mesh != null)
        {
            mesh.Clear();
        }
        // Remove the box collider
        BoxCollider collider = GetComponent<BoxCollider>();
        if (collider != null)
        {
            DestroyImmediate(collider);
        }
        DisableRendering();
    }

    // Helper method to disable rendering
    private void DisableRendering()
    {
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.enabled = false;
        }
        MeshFilter filter = GetComponent<MeshFilter>();
        if (filter != null && filter.mesh != null) 
        {
            if (Application.isPlaying)
            {
                if (filter.mesh != null) filter.mesh.Clear(); 
            }
            else
            {
                if (filter.sharedMesh != null) filter.sharedMesh.Clear();
            }
        }
    }

    // Helper method to enable rendering
    private void EnableRendering()
    {
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.enabled = true;
        }
    }

    //---------------Triangle creation the heart of everything. Pay attention here !!--------------
    private void CreateTriangles()
    {
        int[] trianglesZ = new int[(xSize * ySize) * 12];
        int[] trianglesX = new int[(ySize * zSize) * 12];
        int[] trianglesY = new int[(xSize * zSize) * 12];
        int ring = (xSize + zSize) * 2;

        int tZ = 0, tX = 0, tY = 0, v = 0;

        for (int y = 0; y < ySize; y++, v++)
        {
            for (int q = 0; q < xSize; q++, v++)
            {
                tZ = SetQuad(trianglesZ, tZ, v, v + 1, v + ring, v + ring + 1);
            }
            for (int q = 0; q < zSize; q++, v++)
            {
                tX = SetQuad(trianglesX, tX, v, v + 1, v + ring, v + ring + 1);
            }
            for (int q = 0; q < xSize; q++, v++)
            {
                tZ = SetQuad(trianglesZ, tZ, v, v + 1, v + ring, v + ring + 1);
            }
            for (int q = 0; q < zSize - 1; q++, v++)
            {
                tX = SetQuad(trianglesX, tX, v, v + 1, v + ring, v + ring + 1);
            }
            tX = SetQuad(trianglesX, tX, v, v - ring + 1, v + ring, v + 1);
        }

        tY = CreateTopFace(trianglesY, tY, ring);
        tY = CreateBottomFace(trianglesY, tY, ring);

        mesh.subMeshCount = 3;
        mesh.SetTriangles(trianglesZ, 0);
        mesh.SetTriangles(trianglesX, 1);
        mesh.SetTriangles(trianglesY, 2);
    }
    private static int SetQuad(int[] triangles, int i, int v00, int v10, int v01, int v11)
    {
        triangles[i] = v00;
        triangles[i + 1] = triangles[i + 4] = v01;
        triangles[i + 2] = triangles[i + 3] = v10;
        triangles[i + 5] = v11;
        return i + 6;
    }

    private int CreateTopFace(int[] triangles, int t, int ring)
    {
        int v = ring * ySize;
        for (int x = 0; x < xSize - 1; x++, v++)
        {
            t = SetQuad(triangles, t, v, v + 1, v + ring - 1, v + ring);
        }
        t = SetQuad(triangles, t, v, v + 1, v + ring - 1, v + 2);

        int vMin = ring * (ySize + 1) - 1;
        int vMid = vMin + 1;
        int vMax = v + 2;

        for (int z = 1; z < zSize - 1; z++, vMin--, vMid++, vMax++)
        {
            t = SetQuad(triangles, t, vMin, vMid, vMin - 1, vMid + xSize - 1);
            for (int x = 1; x < xSize - 1; x++, vMid++)
            {
                t = SetQuad(triangles, t, vMid, vMid + 1, vMid + xSize - 1, vMid + xSize);
            }
            t = SetQuad(triangles, t, vMid, vMax, vMid + xSize - 1, vMax + 1);
        }

        int vTop = vMin - 2;
        t = SetQuad(triangles, t, vMin, vMid, vTop + 1, vTop);
        for (int x = 1; x < xSize - 1; x++, vTop--, vMid++)
        {
            t = SetQuad(triangles, t, vMid, vMid + 1, vTop, vTop - 1);
        }

        t = SetQuad(triangles, t, vMid, vTop - 2, vTop, vTop - 1);

        return t;
    }

    private int CreateBottomFace(int[] triangles, int t, int ring)
    {
        int v = 1;
        int vMid = vertices.Length - (xSize - 1) * (zSize - 1);
        t = SetQuad(triangles, t, ring - 1, vMid, 0, 1);
        for (int x = 1; x < xSize - 1; x++, v++, vMid++)
        {
            t = SetQuad(triangles, t, vMid, vMid + 1, v, v + 1);
        }
        t = SetQuad(triangles, t, vMid, v + 2, v, v + 1);

        int vMin = ring - 2;
        vMid -= xSize - 2;
        int vMax = v + 2;

        for (int z = 1; z < zSize - 1; z++, vMin--, vMid++, vMax++)
        {
            t = SetQuad(triangles, t, vMin, vMid + xSize - 1, vMin + 1, vMid);
            for (int x = 1; x < xSize - 1; x++, vMid++)
            {
                t = SetQuad(
                    triangles, t,
                    vMid + xSize - 1, vMid + xSize, vMid, vMid + 1);
            }
            t = SetQuad(triangles, t, vMid + xSize - 1, vMax + 1, vMid, vMax);
        }

        int vTop = vMin - 1;
        t = SetQuad(triangles, t, vTop + 1, vTop, vTop + 2, vMid);
        for (int x = 1; x < xSize - 1; x++, vTop--, vMid++)
        {
            t = SetQuad(triangles, t, vTop, vTop - 1, vMid, vMid + 1);
        }
        t = SetQuad(triangles, t, vTop, vTop - 1, vMid, vTop - 2);

        return t;
    }

    // IMPORTANT:Center the collider !!!!
    private void CreateColliders()
    {
        // Check for existing collider first
        BoxCollider existingCollider = gameObject.GetComponent<BoxCollider>();
        BoxCollider c = existingCollider != null ? existingCollider : gameObject.AddComponent<BoxCollider>();

        // Since the mesh is now centered at origin, the collider center should be at (0,0,0)
        c.center = Vector3.zero;

        // The actual size of the mesh remains the same
        c.size = new Vector3(xSize - 0.2f, ySize - 0.2f, zSize - 0.2f);
    }
}