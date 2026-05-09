using UnityEngine;
using static AtlasSpawn;
using static Atlas;
[CreateAssetMenu(fileName = "Trip", menuName = "Atlas / Trip")]
public class TripSO : ScriptableObject
{
    public ParticleAtlas[] particleAtlasArray;

    public StationSO[] stationsDataArray;
    public NPCBrain[] npc_prefabsArray;

    [Header("Settings")]
    public int minStationsTraitorsTravel = 2;
    public int maxStationsTraitorsTravel = 4;
    [Header("Generated")]
    public StationSO nextStation;

    public int ticketsCheckedSinceLastStation;
}
