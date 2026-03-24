using UnityEngine;

[CreateAssetMenu(fileName = "CameraStats_SO", menuName = "Midnight Line SOs / Camera Stats SO")]
public class CameraStatsSO : ScriptableObject
{
    public enum State
    {
        None,
        Station,
        Carriage,
        Roof,
        Gangway,
    }

    public State curState;

    public float initialSize;
    public float targetSize;
    public float camHeight;
    public float camWidth;
    public float curHorOffset;
    public float camWorldLeft;
    public float camWorldRight;
    public float camWorldBottom;
    public float camWorldTop;
    public float worldUnitsPerPixel;
    public Vector3 curWorldPos;
    public Vector3 prevWorldPos;
    public float aspect;
    public Vector3 targetWorldPos;

    public Vector3 curVelocity;
}
