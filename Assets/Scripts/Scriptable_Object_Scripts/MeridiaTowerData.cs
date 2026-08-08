using UnityEngine;

[CreateAssetMenu(fileName = "MeridiaTowerData", menuName = "Midnight Line SOs / Meridia Tower Data")]
public class MeridiaTowerData : ScriptableObject
{
    public Vector2 bottomFloorCenterTopPos;
    public int curLevel;
    public bool elevatorMoving;
}
