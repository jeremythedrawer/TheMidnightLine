using UnityEngine;

[CreateAssetMenu(fileName = "Station_SO", menuName = "Midnight Line SOs / Station SO")]
public class StationSO : ScriptableObject
{
    public int stationQueuePosition;
    public int newTrainSpeed = 100;
    public int metersPosition = 0;
    public int npcSpawnAmount = 10;
}
