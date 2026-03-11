using UnityEngine;
using static Parallax;
using static Atlas;
public class ParallaxController : MonoBehaviour
{
    public CameraStatsSO camStats;
    public TrainStatsSO trainStats;
    public SpyStatsSO spyStats;
    public ZoneSpawnerStatsSO zoneStats;
    public AtlasRenderer atlasRenderer;
    public bool ignoreParallax;
    public Parallax.RepeatType repeatType;
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
        float worldPivot = (atlasRenderer.sprite.uvPivot.x * atlasRenderer.atlas.texture.width) / PIXELS_PER_UNIT;
        pivotToMaxWorldDist = atlasRenderer.worldWidth - worldPivot;
        minWorldToPivotDist = atlasRenderer.worldWidth - pivotToMaxWorldDist;
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
