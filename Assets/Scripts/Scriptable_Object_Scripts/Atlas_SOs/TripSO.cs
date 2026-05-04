using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static AtlasSpawn;
using static Atlas;
[CreateAssetMenu(fileName = "Trip", menuName = "Atlas / Trip")]
public class TripSO : ScriptableObject
{
    public ZoneAreaSO[] zoneAreas;
    public StationSO[] stationsDataArray;
    public NPCBrain[] npc_prefabsArray;
    public ScrollSprite[] scrollSprites;

    [Header("Settings")]
    public int minStationsTraitorsTravel = 2;
    public int maxStationsTraitorsTravel = 4;
    [Header("Generated")]
    public StationSO nextStation;
    
    public int totalTicketsToCheck;
    public int ticketsCheckedSinceLastStation;
}
