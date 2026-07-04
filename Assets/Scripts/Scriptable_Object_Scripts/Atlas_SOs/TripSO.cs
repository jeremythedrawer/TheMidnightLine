using UnityEngine;

using static AtlasUI;
using static NPC;

[CreateAssetMenu(fileName = "Trip", menuName = "Atlas / Trip")]
public class TripSO : ScriptableObject
{
    public TripPrompt prompt;
    public TripClue clue;
    public ParticleAtlas[] particleAtlasArray;
    public StationSO[] stationsDataArray;
    public NPCSO[] npcDataArray;

    public float[] dayNightValues;
    public Vector2[] elevationValues;
    public float[] kmValues;

    [Header("Generated")]
    public StationSO stationAhead;

    public Color[] selectedClueMarkerColors;
    
    public TraitorProfile[] traitorProfiles;

    public int ticketsCheckedSinceLastStation;
    public int traitorsSpawned;
    public int unlockedColorMarkerCount;
    public int selectedColorMarkerIndex;

    public UnlockType curUnlocks;


}
