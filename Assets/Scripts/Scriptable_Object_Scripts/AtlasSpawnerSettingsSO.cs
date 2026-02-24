using UnityEngine;
[CreateAssetMenu(fileName = "SpawnerSettingsSO", menuName = "Midnight Line SOs / Spawner Settings SO")]
public class AtlasSpawnerSettingsSO : ScriptableObject
{
    public TripSO currentTrip;
    public Vector3 spawnerSize = new Vector2(32,32);
    public float spawnHeight = 0;
    public ComputeShader atlasCompute;
    public Material backgroundMaterial;
}
