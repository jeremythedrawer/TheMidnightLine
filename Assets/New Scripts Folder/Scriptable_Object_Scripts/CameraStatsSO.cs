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

    internal float initialSize;
    internal float curHorOffset;
    internal float targetSize;
    internal Vector2 targetWorldPos;
}
