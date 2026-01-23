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
    public float lodPosition0;
    public float lodPosition1;

    public int kernel;
    public int computeGroups;

    public ComputeBuffer particleComputeBuffer;
    public ComputeBuffer backgroundMaskBuffer;
    public Bounds renderParamsBounds;
    public Spawn.BackgroundType curBackgroundTypes;

    public int backgroundMaskCount;
    public int[] backgroundMasksArray;

}
