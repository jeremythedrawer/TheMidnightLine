using UnityEngine;

[CreateAssetMenu(fileName = "MeridiaTowerData", menuName = "Midnight Line SOs / Meridia Tower Data")]
public class MeridiaTowerData : ScriptableObject
{
    public Vector2 bottomFloorCenterTopPos;
    public MeetingDoor.Level curLevel;
    public bool elevatorMoving;
}
