using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class S07_DepthTest2 : MonoBehaviour
{
    [SerializeField] private int canvasWidth = 256;
    [SerializeField] private int canvasHeight = 256;

    // 삼각형 1 : 깊이를 0.5로 일정하게 설정
    [SerializeField] private Vector3 vertexA1 = new Vector3(100, 180, 0.5f);
    [SerializeField] private Vector3 vertexB1 = new Vector3(60, 80, 0.5f);
    [SerializeField] private Vector3 vertexC1 = new Vector3(180, 80, 0.5f);
    [SerializeField] private Color color1 = new Color(1f, 0.4f, 0.2f, 1f);

    // 삼각형 2 : 위쪽은 앞, 아래쪽은 뒤가 되도록 z값 설정
    [SerializeField] private Vector3 vertexA2 = new Vector3(150, 200, 0.0f);
    [SerializeField] private Vector3 vertexB2 = new Vector3(90, 60, 0.7f);
    [SerializeField] private Vector3 vertexC2 = new Vector3(220, 60, 0.7f);
    [SerializeField] private Color color2 = new Color(0.2f, 0.5f, 1f, 1f);

    // 삼각형 3 : 비교를 방해하지 않도록 뒤쪽에 배치
    [SerializeField] private Vector3 vertexA3 = new Vector3(210, 220, 0.95f);
    [SerializeField] private Vector3 vertexB3 = new Vector3(160, 50, 0.95f);
    [SerializeField] private Vector3 vertexC3 = new Vector3(250, 50, 0.95f);
    [SerializeField] private Color color3 = new Color(0.3f, 0.9f, 0.4f, 1f);

    private Texture2D canvasTexture;
    private RawImage targetImage;
    private float[,] depthBuffer;

    void OnEnable()
    {
        RedrawAll();
    }

    void OnValidate()
    {
        RedrawAll();
    }

    private void RedrawAll()
    {
        targetImage = GetComponent<RawImage>();
        if (targetImage == null)
            return;

        if (canvasTexture == null ||
            canvasTexture.width != canvasWidth ||
            canvasTexture.height != canvasHeight)
        {
            canvasTexture = new Texture2D(canvasWidth, canvasHeight);
            canvasTexture.filterMode = FilterMode.Point;
        }

        depthBuffer = new float[canvasWidth, canvasHeight];

        for (int x = 0; x < canvasWidth; x++)
        {
            for (int y = 0; y < canvasHeight; y++)
            {
                depthBuffer[x, y] = float.MaxValue;

                // 이전 그림이 남지 않도록 배경 초기화
                canvasTexture.SetPixel(x, y, Color.gray);
            }
        }

        DrawTriangle(vertexA1, vertexB1, vertexC1, color1);
        DrawTriangle(vertexA2, vertexB2, vertexC2, color2);
        DrawTriangle(vertexA3, vertexB3, vertexC3, color3);

        canvasTexture.Apply();
        targetImage.texture = canvasTexture;
    }

    private bool GetBarycentric(
        Vector2 p,
        Vector2 a,
        Vector2 b,
        Vector2 c,
        out float w1,
        out float w2,
        out float w3)
    {
        float denom =
            a.x * (b.y - c.y) +
            b.x * (c.y - a.y) +
            c.x * (a.y - b.y);

        w1 =
            (p.x * (b.y - c.y) +
             b.x * (c.y - p.y) +
             c.x * (p.y - b.y)) / denom;

        w2 =
            (a.x * (p.y - c.y) +
             p.x * (c.y - a.y) +
             c.x * (a.y - p.y)) / denom;

        w3 = 1f - w1 - w2;

        return w1 >= 0f && w2 >= 0f && w3 >= 0f;
    }

    private void DrawTriangle(Vector3 a, Vector3 b, Vector3 c, Color color)
    {
        Vector2 a2 = new Vector2(a.x, a.y);
        Vector2 b2 = new Vector2(b.x, b.y);
        Vector2 c2 = new Vector2(c.x, c.y);

        for (int x = 0; x < canvasWidth; x++)
        {
            for (int y = 0; y < canvasHeight; y++)
            {
                Vector2 pixelCenter =
                    new Vector2(x + 0.5f, y + 0.5f);

                float w1, w2, w3;

                bool isInside = GetBarycentric(
                    pixelCenter,
                    a2,
                    b2,
                    c2,
                    out w1,
                    out w2,
                    out w3
                );

                if (isInside)
                {
                    // TODO 1 완성:
                    // barycentric weight로 현재 픽셀의 z값 보간
                    float interpolatedZ =
                        w1 * a.z +
                        w2 * b.z +
                        w3 * c.z;

                    // TODO 2 완성:
                    // 기존 픽셀보다 가까운 경우에만 색과 depth 갱신
                    if (interpolatedZ < depthBuffer[x, y])
                    {
                        depthBuffer[x, y] = interpolatedZ;
                        canvasTexture.SetPixel(x, y, color);
                    }
                }
            }
        }
    }
}