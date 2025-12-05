using UnityEngine;

[CreateAssetMenu(fileName = "StationsDataSO", menuName = "Midnight Line SOs / Stations Data SO")]
public class StationsDataSO : ScriptableObject
{
    public StationSO[] stations;
}
