using UnityEngine;
using static AtlasUI;
using static Spy;

[CreateAssetMenu(fileName = "CameraStats_SO", menuName = "Midnight Line SOs / Camera Stats SO")]
public class CameraData : ScriptableObject
{
    public float horizontalOffset = 10f;
    public float tripTitleTime = 10f;

    public Bounds bounds;

    public Vector3 curWorldPos;
    public Vector3 prevWorldPos;
    public Vector3 curVelocity;

    public Bounds curLocationBounds;
    public LocationState curLocationState;
    public UIState curUIState;

    public float worldUnitsPerPixel;
    public float tripTitleMenuClock;

}
