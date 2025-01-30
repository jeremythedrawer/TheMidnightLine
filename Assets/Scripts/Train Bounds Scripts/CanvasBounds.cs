using UnityEngine;

public class CanvasBounds : Bounds
{
    [Header("References")]
    public TrainBounds trainBounds;
    public Camera cam;

    [Header("Parameters")]
    public float canvasHeight;
    public float canvasWidthBuffer;

    [Range(0, 1)]
    public float backgroundDepthMin = 0.9f;
    [Range(0, 1)]
    public float backgroundDepthMax = 0.6f;

    public float minDepthNormalized {  get; private set; }
    public float maxDepthNormalized {  get; private set; }
    public float oneThirdClipPlane { get; private set; }
    public float twoThirdsClipPlane { get; private set; }

    private float camHalfWidth => cam.orthographicSize * cam.aspect;
    private float top => transform.position.y + canvasHeight;
    private float bottom => (transform.position.y - canvasHeight);
    private float left => (trainBounds.boundsMinX - camHalfWidth) - canvasWidthBuffer;
    private  float right => (trainBounds.boundsMaxX + camHalfWidth) + canvasWidthBuffer;

    public Vector3 spawnPoint { get; private set; }
    public Vector3 despawnPoint { get; private set; }

    private Vector2 topRight => new Vector2(right, top);
    private Vector2 bottomLeft => new Vector2(left, bottom);

    private void OnValidate()
    {
        minDepthNormalized = Mathf.Lerp(cam.nearClipPlane, cam.farClipPlane, backgroundDepthMin);
        maxDepthNormalized = Mathf.Lerp(cam.nearClipPlane, cam.farClipPlane, backgroundDepthMax);
        oneThirdClipPlane = (cam.farClipPlane - cam.nearClipPlane) * 0.333f;
        twoThirdsClipPlane = (cam.farClipPlane - cam.nearClipPlane) * 0.667f;

    }

    private void OnDrawGizmos()
    {
        Helpers.DrawSquare(topRight, bottomLeft, Color.black, cam.farClipPlane, true);
        Helpers.DrawSquare(topRight, bottomLeft, Color.green, oneThirdClipPlane, true);
        Helpers.DrawSquare(topRight, bottomLeft, Color.green, twoThirdsClipPlane, true);
        Helpers.DrawSquare(topRight, bottomLeft, Color.black, cam.nearClipPlane, true);

        Helpers.DrawSquare(topRight, bottomLeft, Color.white, minDepthNormalized, true);
        Helpers.DrawSquare(topRight, bottomLeft, Color.white, maxDepthNormalized, true);
    }

    private void Awake()
    {
        spawnPoint = new Vector3(right, transform.position.y, cam.farClipPlane);
        despawnPoint = new Vector3(left, transform.position.y, cam.farClipPlane);        
    }
    void Start()
    {
        Helpers.DrawSquare(topRight, bottomLeft, Color.blue, cam.farClipPlane, false);
    }
}
