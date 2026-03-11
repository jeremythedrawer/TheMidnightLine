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
    public float camLeft;
    public float camRight;
    public float camBottom;
    public float camTop;
    public Vector3 curWorldPos;
    public Vector3 prevWorldPos;
    public float aspect;
    public Vector2 targetWorldPos;

    public Vector3 curVelocity;
}
