using UnityEngine;
using static Parallax;
using static Atlas;
public class ParallaxController : MonoBehaviour
{
    public CameraStatsSO camStats;
    public TrainStatsSO trainStats;
    public SpyStatsSO spyStats;
    public bool ignoreParallax;
    [Header("Generated")]
    public float parallaxFactor;
    public Vector3 worldPos;
    private void Start()
    {
        worldPos = transform.position;
        if (!ignoreParallax)
        {
            parallaxFactor = GetParallaxFactor(transform.position.z);
        }
    }

    private void Update()
    {
        if (!ignoreParallax)
        {
            worldPos.x -= UpdateParallaxPosition(camStats,spyStats , trainStats, parallaxFactor);
        }
        else if (spyStats.onTrain)
        { 
            worldPos.x -= UpdatePositionNoParallax(trainStats);
        }
        worldPos.x = Mathf.Round(worldPos.x * PIXELS_PER_UNIT) * UNITS_PER_PIXEL;
        transform.position = worldPos;   
    }
}
