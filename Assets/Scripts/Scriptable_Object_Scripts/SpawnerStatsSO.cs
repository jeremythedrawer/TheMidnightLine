using System;
using UnityEngine;

[CreateAssetMenu(fileName = "SpawnerStatsSO", menuName = "Midnight Line SOs / Spawner Stats SO")]
public class SpawnerStatsSO : ScriptableObject
{    
    public int curBackgroundDataIndex;
    public Vector3 topRightFront;
    public Vector3 bottomLeftFront;
    public Vector3 topRightBack;
    public Vector3 bottomLeftBack;
    public Vector3[] lodPositions;

    public int kernel;
    public int computeGroups;

    public ComputeBuffer backgroundComputeBuffer;
    public Bounds renderParamsBounds;
    public Spawn.BackgroundType curBackgroundTypes;

    public int spawnerMask;

}
