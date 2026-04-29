using UnityEngine;

[CreateAssetMenu(fileName = "CameraStats_SO", menuName = "Midnight Line SOs / Camera Stats SO")]
public class CameraStatsSO : ScriptableObject
{
    public Bounds camBounds;

    public float worldUnitsPerPixel;
    public Vector3 curWorldPos;
    public Vector3 prevWorldPos;
    public Vector3 curVelocity;
}
