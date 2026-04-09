using UnityEngine;
using static AtlasSpawn;
[CreateAssetMenu(fileName = "ZoneSpawnerSO", menuName = "Midnight Line SOs / Zone Spawner SO")]
public class ZoneSpawnerSO : ScriptableObject
{
    public ComputeShader atlasCompute;
    public Material material;

    [Header("Generated")]
    public int curBackgroundDataIndex;

    public Bounds bounds;
}
