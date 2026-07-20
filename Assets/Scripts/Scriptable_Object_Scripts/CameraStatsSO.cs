using UnityEngine;

[CreateAssetMenu(fileName = "CameraStats_SO", menuName = "Midnight Line SOs / Camera Stats SO")]
public class CameraStatsSO : ScriptableObject
{
    public Matrix4x4 worldToCam;
    public Matrix4x4 camToWorld;

    public Bounds camBounds;
    public Bounds meetingBounds;
    public Bounds bunkerBounds;

    public Vector3 curWorldPos;
    public Vector3 prevWorldPos;
    public Vector3 curVelocity;

    public float worldUnitsPerPixel;
}
