using System.Collections.Generic;
using UnityEngine;
using static AtlasSpawn;

[CreateAssetMenu(fileName = "Trip", menuName = "Atlas / Trip")]
public class TripSO : ScriptableObject
{
    public Zone[] zones;
    public Queue<Zone> zoneQueue;
    public StationSO[] stations;

    [Header("Generated")]
    public int curStationIndex;
    public StationSO curStation;
    public int tripMeters;
}
