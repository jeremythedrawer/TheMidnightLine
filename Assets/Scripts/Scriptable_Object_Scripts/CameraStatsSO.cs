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
    public float curHorOffset;
    public float targetSize;
    public Vector3 curWorldPos;
    public Vector3 prevWorldPos;
    public float aspect;
    public Vector2 targetWorldPos;

    public Vector3 curVelocity;
}
