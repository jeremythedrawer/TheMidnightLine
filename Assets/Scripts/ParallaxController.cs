using UnityEngine;
using static Parallax;
using static Atlas;
using static Spy;
public class ParallaxController : MonoBehaviour
{
    public TrainStatsSO trainStats;
    public ZoneSpawnerSO zoneStats;
    public SpyStatsSO spyStats;
    public AtlasRenderer leftRenderer;

    public Parallax.RepeatType repeatType;
    
    public bool ignoreParallax;
    
    [Header("Using Parallax")]
    public CameraStatsSO camStats;

    [Header("Multiple Sprites")]
    public AtlasRenderer rightRenderer;

    [Header("Generated")]
    public float parallaxFactor;
    public Vector3 worldPos;
    public Bounds bounds;
    public Vector3 boundsOffset;
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
        if (!ignoreParallax)
        {
            parallaxFactor = GetParallaxFactor(transform.position.z);
        }
    }

    private void FixedUpdate()
    {
        if (!ignoreParallax)
        {
            float parallaxIncrement = UpdateParallaxPosition(camStats, spyStats, trainStats, parallaxFactor);

            worldPos.x -= parallaxIncrement;
            bounds.center = worldPos + boundsOffset;
        }
        else if (spyStats.curLocationState != LocationState.Station)
        {
            worldPos.x -= UpdatePositionNoParallax(trainStats);
        }
        worldPos.x = Mathf.Round(worldPos.x * PIXELS_PER_UNIT) * UNITS_PER_PIXEL;
        transform.position = worldPos;

        if (bounds.max.x < zoneStats.bounds.min.x)
        {
            switch (repeatType)
            {
                case RepeatType.OneShot:
                {
                   // Destroy(gameObject);
                }
                break;
                case RepeatType.Repeat:
                {
                    worldPos.x += zoneStats.bounds.max.x - bounds.min.x;
                }
                break;
            }
        }
    }
}
