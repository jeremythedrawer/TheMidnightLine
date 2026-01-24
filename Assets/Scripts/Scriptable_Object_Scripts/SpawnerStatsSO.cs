using System;
using UnityEngine;

[CreateAssetMenu(fileName = "SpawnerStatsSO", menuName = "Midnight Line SOs / Spawner Stats SO")]
public class SpawnerStatsSO : ScriptableObject
{    
    public int curBackgroundDataIndex;
    
    public Vector3 spawnMaxPos;
    public Vector3 spawnMinPos;
    public Vector3 spawnCenter;
    public Vector3 spawnSize;

    public float lodZPosition0;
    public float lodZPosition1;

    public int updateKernelID;
    public int initKernelID;
    public int computeGroups;

    public ComputeBuffer particleComputeBuffer;
    public ComputeBuffer backgroundParticleInputBuffer;
    public Bounds renderParamsBounds;
    public Spawn.BackgroundType curBackgroundTypes;

    public int backgroundMaskCount;
    public Spawn.BackgroundParticleInputs[] backgroundInputsArray;

}
