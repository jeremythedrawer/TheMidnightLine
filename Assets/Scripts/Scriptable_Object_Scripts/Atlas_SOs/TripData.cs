using UnityEngine;

using static AtlasUI;
using static Passenger;

[CreateAssetMenu(fileName = "Trip", menuName = "Data / Trip")]
public class TripData : ScriptableObject
{
    public string title;

    public TripPrompt prompt;
    public TripClue clue;
    public ParticleAtlas[] particleAtlasArray;
    public StationSO[] stationsDataArray;
    public PassengerData[] passengers;

    public float[] dayNightValues;
    public Vector2[] elevationValues;
    public float[] kmValues;

    [Header("Generated")]
    public StationSO stationAhead;
    
    public TraitorProfile[] traitorProfiles;

    public int ticketsCheckedSinceLastStation;
    public int ticketsCheckedTotal;
    public int traitorsSpawned;

    public UnlockType curUnlocks;

    public float curDayNightValue;

    public bool unlocked;
    public bool completed;
}
