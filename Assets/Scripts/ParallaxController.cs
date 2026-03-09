using UnityEngine;
using static Parallax;
using static Atlas;
public class ParallaxController : MonoBehaviour
{
    public CameraStatsSO camStats;
    public TrainStatsSO trainStats;
    public SpyStatsSO spyStats;
    [Header("Generated")]
    public float parallaxFactor;
    public Vector3 worldPos;
    private void Start()
    {
        worldPos = transform.position;
        parallaxFactor = GetParallaxFactor(transform.position.z);
    }

    private void Update()
    {
        worldPos.x -= UpdateParallaxPosition(camStats,spyStats , trainStats, parallaxFactor);
        worldPos.x = Mathf.Round(worldPos.x * PIXELS_PER_UNIT) * UNITS_PER_PIXEL;
        transform.position = worldPos;   
    }
}
