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

    public float[] lodThresholds;
    public int[] lodWriteOffsets;

    public int updateKernelID;
    public int initKernelID;
    public int computeGroups;

    public ComputeBuffer outputComputeBuffer;
    public ComputeBuffer inputComputeBuffer;
    public ComputeBuffer lodWriteOffsetsBuffer;
    public ComputeBuffer lodThresholdBuffer;
    public ComputeBuffer[] lodBuffers; 

    public Bounds renderParamsBounds;
    public BackgroundType curBackgroundTypes;

    //public GraphicsBuffer indexBuffer;
    
    public int backgroundMaskCount;
    public BackgroundParticleInputs[] backgroundInputsArray;

    public SpawnerData[] spawnerDataArray = new SpawnerData[SPAWNER_COUNT];

    public Dictionary<BackgroundType, ParticleData> particleDataDict;
}
