
using UnityEngine;
using static AtlasSpawn;

[CreateAssetMenu(fileName = "SpawnerStats", menuName = "Atlas / Spawner Stats")]
public class AtlasSpawnerStatsSO : ScriptableObject
{
    [Header("Generated")]
    public int curBackgroundDataIndex;
    
    public Vector3 spawnMaxPos;
    public Vector3 spawnMinPos;
    public Vector3 spawnCenter;
    public Vector3 spawnBoundsSize;

    public int computeGroups;
    public float trainToMaxBoundDist;

    public SpawnerData[] spawnerDataArray;
}
