using UnityEngine;
[CreateAssetMenu(fileName = "ZoneSpawnerSO", menuName = "Midnight Line SOs / Zone Spawner SO")]
public class SpawnSO : ScriptableObject
{
    public ComputeShader zoneCompute;
    public ComputeShader scrollCompute;
    public Material zoneMaterial;
    public Material scrollMaterial;
    public Scroller scroller_prefab;
    [Header("Generated")]
    public Bounds bounds;

    public uint[] moveInputs;
    public ComputeBuffer scrollMoveInputBuffer;
    
    public int scrollKernelInit;
    public int scrollKernelUpdate;
    public int scrollComputeGroupSize;
}
