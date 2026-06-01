using UnityEngine;
using static AtlasSpawn;
using static Atlas;
[CreateAssetMenu(fileName = "Trip", menuName = "Atlas / Trip")]
public class TripSO : ScriptableObject
{
    public ParticleAtlas[] particleAtlasArray;
    public StationSO[] stationsDataArray;
    public NPCBrain[] npc_prefabsArray;

    public float[] dayNightValues;
    public Vector2[] elevationValues;
    public float[] kmValues;

    [Header("Settings")]
    public int minStationsTraitorsTravel = 2;
    public int maxStationsTraitorsTravel = 4;
    [Header("Generated")]
    public StationSO nextStation;
    public int ticketsCheckedSinceLastStation;
}
