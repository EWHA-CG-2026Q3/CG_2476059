using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class S04_CustomTetrahedronMesh_Finish : MonoBehaviour
{
    void Start()
    {
        // 정육면체 8개 정점(S3) 중 0, 2, 5, 7번 — 대각선으로 고르면 정사면체가 됨
        Vector3[] vertices = new Vector3[]
        {
            new Vector3(0f, 0f, 0f),
            new Vector3(1f, 1f, 0f),
            new Vector3(1f, 0f, 1f),
            new Vector3(0f, 1f, 1f),
        };

        int[] triangles = new int[]
        {
            1, 3, 2,
            0, 1, 2,
            0, 2, 3,
            0, 3, 1,
        };

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        GetComponent<MeshFilter>().mesh = mesh;

        // 이미 Inspector에서 재질을 넣었다면 덮어쓰지 않음
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();

        if (meshRenderer.sharedMaterial == null)
        {
            meshRenderer.sharedMaterial =
                new Material(Shader.Find("Universal Render Pipeline/Lit"));
        }
    }
}