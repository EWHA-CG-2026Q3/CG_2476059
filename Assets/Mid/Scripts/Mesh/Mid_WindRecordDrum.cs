using System.Collections.Generic;
using UnityEngine;

/// <summary>Topic 1: a hand-written polygon drum, NOT a Cylinder primitive.</summary>
[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public sealed class Mid_WindRecordDrum : MonoBehaviour
{
    [SerializeField, Range(3, 64)] int sides = 12;
    [SerializeField, Min(0.01f)] float radius = 0.46f;
    [SerializeField, Min(0.01f)] float height = 0.56f;
    Mesh generatedMesh;
    bool rebuild;
    public int Sides => sides;

    void OnEnable() => RebuildMesh();
    // Defer changes until Update; never change renderer/material state in OnValidate.
    void OnValidate() => rebuild = true;
    void Update() { if (rebuild) RebuildMesh(); }

    public void RebuildMesh()
    {
        rebuild = false;
        sides = Mathf.Clamp(sides, 3, 64);
        radius = Mathf.Max(0.01f, radius);
        height = Mathf.Max(0.01f, height);
        if (generatedMesh == null)
            generatedMesh = new Mesh { name = "Mid_ProceduralRecordDrum", hideFlags = HideFlags.DontSave };

        var lower = new Vector3[sides];
        var upper = new Vector3[sides];
        // NEW VERTICES: compute every point on BOTH polygon rings using a loop.
        for (int i = 0; i < sides; i++)
        {
            float angle = 2f * Mathf.PI * i / sides;
            lower[i] = new Vector3(radius * Mathf.Cos(angle), 0f, radius * Mathf.Sin(angle));
            upper[i] = lower[i] + Vector3.up * height;
        }

        var vertices = new List<Vector3>(sides * 10);
        var uv = new List<Vector2>(sides * 10);
        var triangles = new List<int>(sides * 12);
        // NEW TOPOLOGY: two outward-facing side triangles and two cap triangles per interval.
        for (int i = 0; i < sides; i++)
        {
            int next = (i + 1) % sides;
            int start = vertices.Count;
            // Split vertices at facet edges to keep the twelve flat faces visible.
            vertices.Add(lower[i]); vertices.Add(upper[i]);
            vertices.Add(upper[next]); vertices.Add(lower[next]);
            uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(0, 1));
            uv.Add(new Vector2(1, 1)); uv.Add(new Vector2(1, 0));
            triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2);
            triangles.Add(start); triangles.Add(start + 2); triangles.Add(start + 3);
            AddCap(vertices, uv, triangles, Vector3.up * height, upper[next], upper[i]);
            AddCap(vertices, uv, triangles, Vector3.zero, lower[i], lower[next]);
        }
        generatedMesh.Clear();
        generatedMesh.SetVertices(vertices);
        generatedMesh.SetUVs(0, uv);
        generatedMesh.SetTriangles(triangles, 0);
        generatedMesh.RecalculateNormals();
        generatedMesh.RecalculateBounds();
        GetComponent<MeshFilter>().sharedMesh = generatedMesh;
        // Deliberately do NOT assign Renderer.material/sharedMaterial here.
        // The Inspector-assigned Mid material survives rebuilding and entering Play Mode.
    }

    void AddCap(List<Vector3> vertices, List<Vector2> uv, List<int> triangles,
        Vector3 center, Vector3 a, Vector3 b)
    {
        int start = vertices.Count;
        vertices.Add(center); vertices.Add(a); vertices.Add(b);
        uv.Add(new Vector2(0.5f, 0.5f));
        uv.Add(new Vector2(a.x, a.z) / (2 * radius) + Vector2.one * 0.5f);
        uv.Add(new Vector2(b.x, b.z) / (2 * radius) + Vector2.one * 0.5f);
        triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2);
    }

    void OnDisable()
    {
        if (generatedMesh == null) return;
        if (Application.isPlaying) Destroy(generatedMesh); else DestroyImmediate(generatedMesh);
        generatedMesh = null;
    }
}
