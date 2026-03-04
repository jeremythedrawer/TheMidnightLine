
using UnityEngine;
using static AtlasSpawn;

[CreateAssetMenu(fileName = "ZoneSpawnerStats", menuName = "Atlas / Zone Spawner Stats")]
public class ZoneSpawnerStatsSO : ScriptableObject
{
    [Header("Generated")]
    public int curBackgroundDataIndex;
    
    public Vector3 spawnMaxPos;
    public Vector3 spawnMinPos;
    public Vector3 spawnCenter;
    public Vector3 spawnBoundsSize;

    public int computeGroups;
    public float trainToMaxBoundDist;

    public ZoneSpawnerData[] zoneSpawnerDataArray;
}
