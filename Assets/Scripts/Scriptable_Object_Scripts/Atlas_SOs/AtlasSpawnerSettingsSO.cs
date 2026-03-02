using UnityEngine;
[CreateAssetMenu(fileName = "ZoneSpawnerSettingsSO", menuName = "Midnight Line SOs / Zone Settings SO")]
public class AtlasSpawnerSettingsSO : ScriptableObject
{
    public TripSO currentTrip;
    public Vector3 spawnerSize = new Vector2(32,32);
    public float spawnHeight = 0;
    public ComputeShader atlasCompute;
    public Material material;
}
