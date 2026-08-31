using UnityEngine;

[CreateAssetMenu(fileName = "Region", menuName = "Data / Region")]
public class RegionData : ScriptableObject
{
    public TripData[] trips;

    [Header("Generated")]
    public bool unlocked;
}
