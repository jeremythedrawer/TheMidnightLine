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
    public bool ignoreSpawnBounds;
    

    [Range(0, FAR_CLIP)] public float worldDepth;

    [Header("Multiple Sprites")]
    public AtlasRenderer rightRenderer;

    [Header("Generated")]
    public float parallaxFactor;
    public Vector3 worldPos;
    public Bounds bounds;
    public Vector3 boundsOffset;
    public Vector2 velocity;
    private void Start()
    {
        if (ignoreSpawnBounds)
        {
            worldPos = transform.position;
            SetParrallaxFactor();
        }
    }
    private void Update()
    {
        if (bounds.max.x > spawnData.bounds.min.x || ignoreSpawnBounds)
        {
            UpdateWorldPos();
        }
        else
        { 
            Destroy(gameObject);
        }
    }
    private void FixedUpdate()
    {
        bounds.center = worldPos + boundsOffset;
    }
    public void SetParrallaxFactor()
    {
        parallaxFactor = (FAR_CLIP - worldDepth) / FAR_CLIP;
    }
    public void SetWorldPos(Vector2 pos)
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

    public void UpdateWorldPos()
    {
        velocity = camStats.curVelocity * Time.deltaTime * (1 - parallaxFactor);
        if (spyStats.curLocationState != Spy.LocationState.Station)
        {
            velocity += trainStats.curVelocity * Time.deltaTime * parallaxFactor;
        }
        worldPos.x -= velocity.x;
        worldPos.y -= velocity.y;
        worldPos = CameraController.GetSnappedPosition(worldPos, camStats.worldUnitsPerPixel);
        transform.position = worldPos;
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }
}
