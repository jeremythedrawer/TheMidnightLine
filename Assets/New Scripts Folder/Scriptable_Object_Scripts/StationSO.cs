using UnityEngine;

[CreateAssetMenu(fileName = "Station_SO", menuName = "Midnight Line SOs / Station SO")]
public class StationSO : ScriptableObject
{
    public int newTrainSpeed = 100;
    public int metersPosition = 0;
    public int bystanderSpawnAmount = 10;
    public int agentSpawnAmount = 2;
}
