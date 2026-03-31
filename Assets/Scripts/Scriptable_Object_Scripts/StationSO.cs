using System.Collections.Generic;
using UnityEngine;
using static NPC;
[CreateAssetMenu(fileName = "Station_SO", menuName = "Midnight Line SOs / Station SO")]
public class StationSO : ScriptableObject
{
    public Station station_prefab;
    public int targetTrainSpeed = 100;
    public int metersPosition = 0;

    [Range(0, 1)]public float busynessFactor = 0.2f;
    public int traitorSpawnAmount = 2;

    public bool isFrontOfTrain;
    [Header("Generated")]
    public string stationName;
    public bool hadSpawned;
    public  List<NPCProfile> bystanderProfiles;
    public  List<NPCProfile> traitorProfiles;
}
