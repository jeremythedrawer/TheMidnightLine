using UnityEngine;
using static AtlasSpawn;
[CreateAssetMenu(fileName = "ZoneSpawnerSO", menuName = "Midnight Line SOs / Zone Spawner SO")]
public class ZoneSpawnerSO : ScriptableObject
{
    public Vector3 spawnerSize = new Vector2(32,32);
    public float spawnHeight = 0;
    public ComputeShader atlasCompute;
    public Material material;

    [Header("Generated")]
    public int curBackgroundDataIndex;
    public Vector3 spawnMaxPos;
    public Vector3 spawnMinPos;
    public Vector3 spawnCenter;
    public Vector3 spawnBoundsSize;
}
