using UnityEngine;
using static AtlasUI;
using static Spy;

[CreateAssetMenu(fileName = "CameraStats_SO", menuName = "Midnight Line SOs / Camera Stats SO")]
public class CameraData : ScriptableObject
{
    public float horizontalOffset = 10f;
    public float verticalOffset = 2f;

    public float shakeTime = 0.5f;
    public float shakeIntensity = 0.4f;

    [Range(1f, 2f)] public float fallingSizeMultiplier = 1.5f;
    public float fallingOffset = 0.3f;
    public int fallThreshold;
    public float maxProjectionSize = 10.0f;

    public Matrix4x4 worldToCam;
    public Matrix4x4 camToWorld;

    public Bounds bounds;

    public Vector3 curWorldPos;
    public Vector3 prevWorldPos;
    public Vector3 curVelocity;

    public Bounds curLocationBounds;
    public LocationState curLocationState;
    public UIState curUIState;

    public float worldUnitsPerPixel;
}
