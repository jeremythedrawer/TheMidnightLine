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
            float parallaxVelocity = (camStats.curVelocity.x * (1 - parallaxFactor));
            if (spyStats.curLocationState != Spy.LocationState.Station)
            {
                parallaxVelocity += trainStats.curVelocity * Time.deltaTime * parallaxFactor;
            }
            worldPos.x -= parallaxVelocity;
            //worldPos.x = Mathf.Round(worldPos.x * PIXELS_PER_UNIT) * UNITS_PER_PIXEL;
            transform.position = worldPos;
        }
    }
    private void FixedUpdate()
    {
        bounds.center = worldPos + boundsOffset;
    }
}
