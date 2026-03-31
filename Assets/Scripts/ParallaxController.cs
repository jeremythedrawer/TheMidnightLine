using UnityEngine;
using static Parallax;
using static Atlas;
using System;
using TMPro;
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
        
        switch(leftRenderer)
        {
            case AtlasSimpleRenderer simpleRenderer:
            {
                bounds = simpleRenderer.renderInput.bounds;
            }
            break;

            case AtlasMotionRenderer motionRenderer:
            {
                bounds = motionRenderer.renderInput.bounds;
            }
            break;

            case AtlasSliceRenderer sliceRenderer:
            {
                bounds = sliceRenderer.renderInput.bounds;
            }
            break;
        }

        if (rightRenderer)
        {
            Bounds rightBounds = new Bounds();

            switch (rightRenderer)
            {

                case AtlasSimpleRenderer simpleRenderer:
                {
                    rightBounds = simpleRenderer.renderInput.bounds;
                }
                break;

                case AtlasMotionRenderer motionRenderer:
                {
                    rightBounds = motionRenderer.renderInput.bounds;
                }
                break;

                case AtlasSliceRenderer sliceRenderer:
                {
                    rightBounds = sliceRenderer.renderInput.bounds;
                }
                break;
            }

            bounds.Encapsulate(rightBounds);
        }

        boundsOffset = bounds.center - worldPos;
        if (!ignoreParallax)
        {
            parallaxFactor = GetParallaxFactor(transform.position.z);
        }
    }

    private void Update()
    {
        if (!ignoreParallax)
        {
            float parallaxIncrement = UpdateParallaxPosition(camStats, spyStats, trainStats, parallaxFactor);

            worldPos.x -= parallaxIncrement;
            bounds.center = worldPos + boundsOffset;
        }
        else if (spyStats.onTrain)
        {
            worldPos.x -= UpdatePositionNoParallax(trainStats);
        }
        worldPos.x = Mathf.Round(worldPos.x * PIXELS_PER_UNIT) * UNITS_PER_PIXEL;
        transform.position = worldPos;

        if (bounds.max.x < zoneStats.spawnMinPos.x)
        {
            switch (repeatType)
            {
                case RepeatType.OneShot:
                {
                    Destroy(gameObject);
                }
                break;
                case RepeatType.Repeat:
                {
                    worldPos.x += zoneStats.spawnMaxPos.x - bounds.min.x;
                }
                break;
            }
        }
    }
}
