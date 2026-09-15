using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class S04_DiamondMesh : MonoBehaviour
{
    void Start()
    {
        Mesh mesh = new Mesh();
        mesh.name = "Diamond Mesh";

        // 총 6개의 정점
        Vector3[] vertices =
        {
            new Vector3(0.5f, 1.0f, 0.5f), // 0 : Top
            new Vector3(0.5f, 0.0f, 0.5f), // 1 : Bottom
            new Vector3(0.0f, 0.5f, 0.5f), // 2 : Left
            new Vector3(1.0f, 0.5f, 0.5f), // 3 : Right
            new Vector3(0.5f, 0.5f, 0.0f), // 4 : Front
            new Vector3(0.5f, 0.5f, 1.0f)  // 5 : Back
        };

        // 총 8개의 삼각형
        // Winding Order를 바깥쪽 면이 보이도록 설정
        int[] triangles =
        {
            // 위쪽 4면
            0, 4, 2,
            0, 3, 4,
            0, 5, 3,
            0, 2, 5,

            // 아래쪽 4면
            1, 2, 4,
            1, 4, 3,
            1, 3, 5,
            1, 5, 2
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GetComponent<MeshFilter>().mesh = mesh;
    }
}