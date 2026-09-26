using UnityEngine;
using UnityEngine.VFX;
using static Atlas;
using static Passenger;

[CreateAssetMenu(fileName = "name_BehaviourContext", menuName = "Midnight Line SOs / NPC Behaviour Context")]
public class HabitData : ScriptableObject
{
    public VisualEffect glyphPrefab;

    [TextArea(3, 10)] public string wasFoundSentence;

    public float minTime;
    public float maxTime;

    public Habits habit;
    public PassengerMotion sittingPrepMotion;
    public PassengerMotion sittingMotion;
    public PassengerMotion standingPrepMotion;
    public PassengerMotion standingMotion;
    public PassengerPath pathToTake;
}
