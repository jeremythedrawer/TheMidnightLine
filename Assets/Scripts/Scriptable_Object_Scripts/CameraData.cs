using UnityEngine;
using static AtlasUI;
using static Spy;

[CreateAssetMenu(fileName = "CameraStats_SO", menuName = "Midnight Line SOs / Camera Stats SO")]
public class CameraData : ScriptableObject
{
    public enum LocationState
    {
        None,
        Station,
        Carriage,
        Menu,
        Title,
    }

    public float horizontalOffset = 10f;
    public float tripTitleTime = 10f;
    public float cursorPanningMoveSpeed = 1f;

    public Bounds bounds;

    public Vector3 curWorldPos;
    public Vector3 prevWorldPos;
    public Vector3 curVelocity;

    public Bounds curLocationBounds;
    public LocationState curLocationState;
    public UIState curUIState;

    public float worldUnitsPerPixel;
    public float tripTitleMenuClock;

    public float halfExtentsX;
}
