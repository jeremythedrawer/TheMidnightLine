using System.Collections.Generic;
using UnityEngine;
using static NPC;
[CreateAssetMenu(fileName = "Station_SO", menuName = "Midnight Line SOs / Station SO")]
public class StationSO : ScriptableObject
{
    public Station station_prefab;
    public int targetKMPH = 100;
    public int ticketsToCheckBeforeSpawn = 0;

    [Range(0, 1)]public float busynessFactor = 0.2f;
    public int traitorSpawnAmount = 2;

    public bool isFrontOfTrain;
    [Header("Generated")]
    public string stationName;
    public int stationIndex;
    public float exitLocalPosX;
    public  NPCProfile[] bystanderProfiles;
    public  NPCProfile[] traitorProfiles;
}
