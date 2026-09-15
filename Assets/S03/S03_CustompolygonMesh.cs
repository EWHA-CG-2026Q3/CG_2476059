using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class S03_CustomPolygonMesh : MonoBehaviour
{
    void Start()
    {
        Mesh mesh = new Mesh();
        mesh.name = "Custom Pentagon";

        // 정점 5개 - 오각형
        Vector3[] vertices =
        {
            new Vector3( 0.0f,  1.5f, 0.0f),   // 0 위
            new Vector3(-1.4f,  0.4f, 0.0f),   // 1 왼쪽 위
            new Vector3(-0.9f, -1.2f, 0.0f),   // 2 왼쪽 아래
            new Vector3( 0.9f, -1.2f, 0.0f),   // 3 오른쪽 아래
            new Vector3( 1.4f,  0.4f, 0.0f)    // 4 오른쪽 위
        };

        // 오각형을 삼각형 3개로 나눔
        int[] triangles =
        {
            0, 1, 2,
            0, 2, 3,
            0, 3, 4
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GetComponent<MeshFilter>().mesh = mesh;
    }
}