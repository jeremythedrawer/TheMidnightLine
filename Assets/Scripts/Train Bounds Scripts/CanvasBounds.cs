using UnityEngine;

public class CanvasBounds : Bounds
{
    [Header("Parameters")]
    public float canvasHeight;
    public float canvasWidthBuffer;

    [Range(0, 1)]
    public float backgroundDepthMin = 0.9f;
    [Range(0, 1)]
    public float backgroundDepthMax = 0.6f;
    public float farClipPlanePos { get; private set; }
    public float nearClipPlanePos { get; private set; }
    private float top => transform.position.y + canvasHeight;
    private float bottom => (transform.position.y - canvasHeight);

    public float left {  get; private set; }
    public float right { get; private set; }

    public float width {  get; private set; }

    public Vector3 farPlaneSpawnPoint { get; private set; }
    public Vector3 nearPlaneSpawnPoint { get; private set; }
    public Vector3 despawnPoint { get; private set; }

    private Vector2 topRight;
    private Vector2 bottomLeft;

    private void OnValidate()
    {
        SetCanvasData();
    }

    private void OnDrawGizmos()
    {
        Helpers.DrawSquare(topRight, bottomLeft, Color.black, farClipPlanePos, true);
        Helpers.DrawSquare(topRight, bottomLeft, Color.black, nearClipPlanePos, true);
    }

    private void Awake()
    {
        farPlaneSpawnPoint = new Vector3(right, transform.position.y, farClipPlanePos);
        nearPlaneSpawnPoint = new Vector3(right, transform.position.y, nearClipPlanePos);
        despawnPoint = new Vector3(left, transform.position.y, farClipPlanePos);
    }

    public void SetCanvasData()
    {
        farClipPlanePos = mainCam.transform.position.z + mainCam.farClipPlane;
        nearClipPlanePos = mainCam.transform.position.z + mainCam.nearClipPlane;

        right = trainData.trainWidth + canvasWidthBuffer;
        left = 0 - canvasWidthBuffer;
        topRight = new Vector2(right, top);
        bottomLeft = new Vector2(left, bottom);
        width = right - left;
    }
}
