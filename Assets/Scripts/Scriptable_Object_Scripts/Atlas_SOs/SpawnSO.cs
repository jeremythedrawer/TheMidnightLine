using UnityEngine;
[CreateAssetMenu(fileName = "ZoneSpawnerSO", menuName = "Midnight Line SOs / Zone Spawner SO")]
public class SpawnSO : ScriptableObject
{
    public ComputeShader zoneCompute;
    public Material zoneMaterial;
    public Material scrollMaterial;
    [Header("Generated")]
    public Bounds bounds;
}
