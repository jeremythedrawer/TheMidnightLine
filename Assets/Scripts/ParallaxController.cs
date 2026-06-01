using UnityEngine;
using static AtlasSpawn;
using static Atlas;
public class ParallaxController : MonoBehaviour
{
    public TrainStatsSO trainStats;
    public SpawnData spawnData;
    public SpyStatsSO spyStats;
    public AtlasRenderer leftRenderer;
    public CameraStatsSO camStats;

    [Range(0, FAR_CLIP)] public float worldDepth;

    [Header("Multiple Sprites")]
    public AtlasRenderer rightRenderer;

    [Header("Generated")]
    public float parallaxFactor;
    public Vector3 worldPos;
    public Bounds bounds;
    public Vector3 boundsOffset;
    private void OnEnable()
    {
        parallaxFactor = (FAR_CLIP - worldDepth) / FAR_CLIP;
    }
    private void Start()
    {
        worldPos = transform.position;
        bounds = leftRenderer.bounds;

        if (rightRenderer)
        {
            Bounds rightBounds = rightRenderer.bounds;
            bounds.Encapsulate(rightBounds);
        }

        boundsOffset = bounds.center - worldPos;
    }

    private void Update()
    {
        if (bounds.max.x > spawnData.bounds.min.x)
        {
            Vector2 parallaxVelocity = (camStats.curVelocity * Time.deltaTime * (1 - parallaxFactor));
            if (spyStats.curLocationState != Spy.LocationState.Station)
            {
                parallaxVelocity += trainStats.curVelocity * Time.deltaTime * parallaxFactor;
            }
            worldPos.x -= parallaxVelocity.x;
            worldPos.y -= parallaxVelocity.y;

        }
    }
    private void LateUpdate()
    {
        transform.position = GetSnappedPosition(worldPos);
    }
    private void FixedUpdate()
    {
        bounds.center = worldPos + boundsOffset;
    }

    private Vector3 GetSnappedPosition(Vector3 pos)
    {
        Matrix4x4 w2c = camStats.worldToCam;
        Matrix4x4 c2w = camStats.camToWorld;

        Vector3 camSpace = w2c.MultiplyPoint3x4(pos);

        camSpace.x = Mathf.Round(camSpace.x / camStats.worldUnitsPerPixel) * camStats.worldUnitsPerPixel;
        camSpace.y = Mathf.Round(camSpace.y / camStats.worldUnitsPerPixel) * camStats.worldUnitsPerPixel;

        Vector3 snappedWorld = c2w.MultiplyPoint3x4(camSpace);
        return snappedWorld;
    }
}
