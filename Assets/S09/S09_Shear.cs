using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(DiamondMesh))]
public class S09_Shear : MonoBehaviour
{
    [SerializeField] private float k = 2f;

    private DiamondMesh diamondMesh;

    void OnEnable()
    {
        diamondMesh = GetComponent<DiamondMesh>();
        ApplyShear();
    }

    void OnValidate()
    {
        if (diamondMesh == null)
            diamondMesh = GetComponent<DiamondMesh>();

        ApplyShear();

        // 과제에서 요구한 꼭대기 정점 결과 출력
        Vector3 topVertex = new Vector3(0.5f, 1f, 0.5f);
        Vector4 h = ToHomogeneous(topVertex);

        float[,] shear = ShearMatrixRaw(k);
        h = MultiplyMatrixVectorRaw(shear, h);

        Debug.Log(
            $"k = {k}, 꼭대기 정점 (0.5, 1, 0.5) → {FromHomogeneous(h)}"
        );
    }

    void Update()
    {
        ApplyShear();
    }

    // y에 비례하여 x 방향으로 밀리는 shear 행렬
    float[,] ShearMatrixRaw(float k)
    {
        return new float[,]
        {
            { 1f, k,  0f, 0f },
            { 0f, 1f, 0f, 0f },
            { 0f, 0f, 1f, 0f },
            { 0f, 0f, 0f, 1f }
        };
    }

    Vector4 ToHomogeneous(Vector3 v)
    {
        return new Vector4(v.x, v.y, v.z, 1f);
    }

    Vector3 FromHomogeneous(Vector4 h)
    {
        return new Vector3(h.x, h.y, h.z);
    }

    Vector4 MultiplyMatrixVectorRaw(float[,] M, Vector4 v)
    {
        float[] input = { v.x, v.y, v.z, v.w };
        float[] result = new float[4];

        for (int row = 0; row < 4; row++)
        {
            for (int col = 0; col < 4; col++)
            {
                result[row] += M[row, col] * input[col];
            }
        }

        return new Vector4(
            result[0],
            result[1],
            result[2],
            result[3]
        );
    }

    void ApplyShear()
    {
        if (diamondMesh == null || diamondMesh.BaseVertices == null)
            return;

        float[,] shear = ShearMatrixRaw(k);

        Vector3[] baseVertices = diamondMesh.BaseVertices;
        Vector3[] verts = new Vector3[baseVertices.Length];

        for (int i = 0; i < baseVertices.Length; i++)
        {
            Vector4 h = ToHomogeneous(baseVertices[i]);
            h = MultiplyMatrixVectorRaw(shear, h);
            verts[i] = FromHomogeneous(h);
        }

        diamondMesh.SetVertices(verts);
    }
}