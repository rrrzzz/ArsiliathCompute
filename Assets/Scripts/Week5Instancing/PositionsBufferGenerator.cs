using UnityEngine;

public class PositionsBufferGenerator : MonoBehaviour
{
    private const int MaxPoints = 10000;

    [SerializeField, Range(0, MaxPoints)]
    private protected int pointCount;

    [SerializeField] private float mod = 60;
    [SerializeField] private float amp = 5.5f;
    [SerializeField] private float freq = 10;
    [SerializeField] private float speed = 64;

    [SerializeField] private float colorFreq = 3000;
    [SerializeField] private float colorSpeed = 1000;

    private protected GraphicsBuffer PosBuffer;
    private protected GraphicsBuffer ColorBuffer;
    
    // Start is called before the first frame update
    private protected virtual void Start()
    {
        PosBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, pointCount, sizeof(float) * 3);
        ColorBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, pointCount, sizeof(float) * 4);
    }

    // Update is called once per frame
    private protected virtual void Update()
    {
        var positions = new Vector3[pointCount];
        var colors = new Vector4[pointCount];
        
        for (int i = 0; i < pointCount; i++)
        {
            positions[i] = new Vector3(i % mod, amp * Mathf.Sin(i / freq + Time.frameCount / speed), i / mod);
            colors[i] = Color.HSVToRGB((i / colorFreq + Time.frameCount / colorSpeed) % 1.0f,.8f, .8f);
        }
        
        PosBuffer.SetData(positions);
        ColorBuffer.SetData(colors);
    }
}
