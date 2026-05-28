using UnityEngine;
using static AtlasSpawn;
[CreateAssetMenu(fileName = "SpawnData", menuName = "Atlas / Spawn Data")]
public class SpawnData : ScriptableObject
{
    [Header("Generated")]
    public bool active;
    public Bounds bounds;

    public SpawnComputeData zoneData;
    public SpawnComputeData scrollData;

    public Material edgeScrollMaterial;
}
