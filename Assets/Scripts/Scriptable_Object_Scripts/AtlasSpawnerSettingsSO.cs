using UnityEngine;
[CreateAssetMenu(fileName = "SpawnerSettingsSO", menuName = "Midnight Line SOs / Spawner Settings SO")]
public class AtlasSpawnerSettingsSO : ScriptableObject
{
    public TripSO currentTrip;
    public float boundBufferAmount = 16f;
    public float spawnHeight = 0;
    public ComputeShader atlasCompute;
    public Material backgroundMaterial;
}
