using System.Collections.Generic;
using UnityEngine;
using static NPC;
[CreateAssetMenu(fileName = "Station_SO", menuName = "Midnight Line SOs / Station SO")]
public class StationSO : ScriptableObject
{
    public Station station_prefab;
    public int ticketsToCheckBeforeSpawn = 0;

    public int bystanderSpawnCount = 10;
    public int traitorSpawnCount = 2;
    public int accompliceSpawnCount = 0;

    public bool isFrontOfTrain;
    [Header("Generated")]
    public string stationName;
    public int stationIndex;
    public float exitLocalPosX;
    public NPCProfile[] bystanderProfiles;
    public NPCProfile[] accompliceProfiles;
}
