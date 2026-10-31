using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter))]
public class S17_AssignDiamondUV : MonoBehaviour
{
    void OnEnable()
    {
        ApplyUV();
    }

    void LateUpdate()
    {
        ApplyUV();
    }

    void ApplyUV()
    {
        Mesh mesh = GetComponent<MeshFilter>().sharedMesh;

        if (mesh == null || mesh.vertexCount != 6)
            return;

        Vector2[] uv = new Vector2[]
        {
            new Vector2(0.5f, 0.5f), // 0 ¾Æ·¡ ²ÀÁþÁ¡
            new Vector2(0f,   0f),   // 1
            new Vector2(1f,   0f),   // 2
            new Vector2(1f,   1f),   // 3
            new Vector2(0f,   1f),   // 4
            new Vector2(0.5f, 0.5f), // 5 À§ ²ÀÁþÁ¡
        };

        mesh.uv = uv;
    }
}