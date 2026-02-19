using System.Collections.Generic;
using UnityEngine;
using static AtlasSpawn;

[CreateAssetMenu(fileName = "SpawnerStats", menuName = "Atlas / Spawner Stats")]
public class AtlasSpawnerStatsSO : ScriptableObject
{
    public AtlasSpawnerSettingsSO settings;
    public MaterialIDSO materialIDs;
    public int curBackgroundDataIndex;
    
    public Vector3 spawnMaxPos;
    public Vector3 spawnMinPos;
    public Vector3 spawnCenter;
    public Vector3 spawnBoundsSize;

    public int computeGroups;
    public SpawnerData[] spawnerDataArray = new SpawnerData[SPAWNER_COUNT];

   // public Dictionary<BackgroundType, ParticleData> particleDataDict;
}
