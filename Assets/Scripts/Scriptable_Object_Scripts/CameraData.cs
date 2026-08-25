using UnityEngine;
using static Spy;

[CreateAssetMenu(fileName = "CameraStats_SO", menuName = "Midnight Line SOs / Camera Stats SO")]
public class CameraData : ScriptableObject
{
    public Matrix4x4 worldToCam;
    public Matrix4x4 camToWorld;

    public Bounds camBounds;

    public Vector3 curWorldPos;
    public Vector3 prevWorldPos;
    public Vector3 curVelocity;

    public Bounds curLocationBounds;
    public LocationState curLocationState;

    public float worldUnitsPerPixel;
}
