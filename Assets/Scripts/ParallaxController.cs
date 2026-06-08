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
    public Vector2 velocity;
    private void OnEnable()
    {
        parallaxFactor = (FAR_CLIP - worldDepth) / FAR_CLIP;
    }
    private void Update()
    {
        if (bounds.max.x > spawnData.bounds.min.x)
        {
            velocity = (camStats.curVelocity * Time.deltaTime * (1 - parallaxFactor));
            if (spyStats.curLocationState != Spy.LocationState.Station)
            {
                velocity += trainStats.curVelocity * Time.deltaTime * parallaxFactor;
            }
            worldPos.x -= velocity.x;
            worldPos.y -= velocity.y;
            transform.position = worldPos;
        }
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

    public void Init(Vector2 pos)
    {
        worldPos.x = pos.x;
        worldPos.y = pos.y;
        worldPos.z = transform.position.z;

        transform.position = worldPos;
        bounds = leftRenderer.GetBounds();
        if (rightRenderer)
        {
            Bounds rightBounds = rightRenderer.GetBounds();
            bounds.Encapsulate(rightBounds);
        }
        boundsOffset = bounds.center - worldPos;
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }
}
