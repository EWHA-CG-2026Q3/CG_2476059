using UnityEngine;

/// <summary>Lecture S23: a surface interpolated in both u and v, with UVs for a leaf texture.</summary>
[ExecuteAlways, RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class S23_BilinearPatch : MonoBehaviour
{
    public Transform corner00, corner10, corner01, corner11;
    [Range(1, 64)] public int uSegments = 20;
    [Range(1, 64)] public int vSegments = 20;
    Mesh generatedMesh;

    void OnEnable() { Rebuild(); }
    void Update() { if (!Application.isPlaying) Rebuild(); }

    public void Rebuild()
    {
        if (!corner00 || !corner10 || !corner01 || !corner11) return;
        if (!generatedMesh)
        {
            generatedMesh = new Mesh { name = name + "_Bilinear", hideFlags = HideFlags.DontSave };
            GetComponent<MeshFilter>().sharedMesh = generatedMesh;
        }
        int us = Mathf.Clamp(uSegments, 1, 64), vs = Mathf.Clamp(vSegments, 1, 64);
        var vertices = new Vector3[(us + 1) * (vs + 1)];
        var uv = new Vector2[vertices.Length];
        for (int j = 0; j <= vs; j++)
        {
            float v = (float)j / vs;
            for (int i = 0; i <= us; i++)
            {
                float u = (float)i / us;
                // P(u,v) = (1-u)(1-v)P00 + u(1-v)P10 + (1-u)vP01 + uvP11
                Vector3 world = (1-u)*(1-v)*corner00.position + u*(1-v)*corner10.position
                              + (1-u)*v*corner01.position + u*v*corner11.position;
                int index = j * (us + 1) + i;
                vertices[index] = transform.InverseTransformPoint(world);
                uv[index] = new Vector2(u, v);
            }
        }
        var triangles = new int[us * vs * 6];
        int t = 0;
        for (int j = 0; j < vs; j++)
            for (int i = 0; i < us; i++)
            {
                int a = j*(us+1)+i, b = a+1, c = a+us+1, d = c+1;
                triangles[t++]=a; triangles[t++]=c; triangles[t++]=b;
                triangles[t++]=b; triangles[t++]=c; triangles[t++]=d;
            }
        generatedMesh.Clear();
        generatedMesh.vertices = vertices;
        generatedMesh.uv = uv;
        generatedMesh.triangles = triangles;
        generatedMesh.RecalculateNormals();
        generatedMesh.RecalculateBounds();
        // Inspector materials are deliberately preserved.
    }

    void OnDisable()
    {
        if (!generatedMesh) return;
        if (Application.isPlaying) Destroy(generatedMesh); else DestroyImmediate(generatedMesh);
    }
}
