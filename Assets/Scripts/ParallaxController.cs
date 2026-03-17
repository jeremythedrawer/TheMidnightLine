using UnityEngine;
using static Parallax;
using static Atlas;
public class ParallaxController : MonoBehaviour
{
    public TrainStatsSO trainStats;
    public ZoneSpawnerSO zoneStats;
    public SpyStatsSO spyStats;
    public AtlasRenderer atlasRenderer;

    public Parallax.RepeatType repeatType;
    
    public bool ignoreParallax;
    
    [Header("Using Parallax")]
    public CameraStatsSO camStats;

    [Header("Multiple Sprites")]
    public AtlasRenderer rightRenderer;
    
    [Header("Generated")]
    public float parallaxFactor;
    public Vector3 worldPos;
    public float pivotToMaxWorldDist;
    public float minWorldToPivotDist;
    private void Start()
    {
        worldPos = transform.position;
        if (!ignoreParallax)
        {
            parallaxFactor = GetParallaxFactor(transform.position.z);
        }
        float totalWidth = atlasRenderer.worldWidth;
        if (rightRenderer != null)
        {
            totalWidth = rightRenderer.bounds.max.x - atlasRenderer.bounds.min.x;
        }
        float worldPivot = (atlasRenderer.sprite.uvPivot.x * atlasRenderer.atlas.texture.width) / PIXELS_PER_UNIT;
        pivotToMaxWorldDist = totalWidth - worldPivot;
        minWorldToPivotDist = totalWidth - pivotToMaxWorldDist;
    }

    private void Update()
    {
        if (!ignoreParallax)
        {
            worldPos.x -= UpdateParallaxPosition(camStats, spyStats, trainStats, parallaxFactor);
        }
        else if (spyStats.onTrain)
        {
            worldPos.x -= UpdatePositionNoParallax(trainStats);
        }
        worldPos.x = Mathf.Round(worldPos.x * PIXELS_PER_UNIT) * UNITS_PER_PIXEL;
        transform.position = worldPos;

        if (worldPos.x + pivotToMaxWorldDist < zoneStats.spawnMinPos.x)
        {
            switch (repeatType)
            {
                case RepeatType.OneShot:
                {
                    Destroy(this.gameObject);
                }
                break;
                case RepeatType.Repeat:
                {
                    worldPos.x = zoneStats.spawnMaxPos.x + minWorldToPivotDist;
                }
                break;
            }
        }
    }
}
