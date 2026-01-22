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

    internal State curState;

    public float initialSize;
    public float curHorOffset;
    public float targetSize;
    public Vector3 curWorldPos;
    public float localFarClipPlane;
    public float worldFarClipPlane;
    public float aspect;
    public Vector2 targetWorldPos;
}
