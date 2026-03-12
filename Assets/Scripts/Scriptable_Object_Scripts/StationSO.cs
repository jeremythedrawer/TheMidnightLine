using UnityEngine;

[CreateAssetMenu(fileName = "Station_SO", menuName = "Midnight Line SOs / Station SO")]
public class StationSO : ScriptableObject
{
    public Station station_prefab;
    public int targetTrainSpeed = 100;
    public int metersPosition = 0;
    public int bystanderSpawnAmount = 10;
    public int traitorSpawnAmount = 2;

    public bool isFrontOfTrain;
    [Header("Generated")]
    public bool hadSpawned;
}
