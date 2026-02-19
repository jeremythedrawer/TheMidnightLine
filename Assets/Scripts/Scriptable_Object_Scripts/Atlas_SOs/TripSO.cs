using UnityEngine;
using static AtlasSpawn;

[CreateAssetMenu(fileName = "Trip", menuName = "Atlas / Trip")]
public class TripSO : ScriptableObject
{
    public Zone[] zones;
    public int tripMeters;
}
