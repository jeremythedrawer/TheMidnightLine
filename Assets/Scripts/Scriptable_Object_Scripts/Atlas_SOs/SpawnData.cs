using UnityEngine;
[CreateAssetMenu(fileName = "SpawnData", menuName = "Atlas / Spawn Data")]
public class SpawnData : ScriptableObject
{
    public ComputeShader zoneCompute;
    public ComputeShader scrollCompute;

    public Material zoneMaterial;
    public Material scrollMaterial;

    [Header("Generated")]
    public bool active;
    public Bounds bounds;

    public Vector2Int[] scrollMoveInputs;
    public int scrollKernelInit;
    public int scrollKernelUpdate;
    public int scrollComputeGroupSize;
    public ComputeBuffer scrollMoveInputBuffer;
    public ComputeBuffer scrollOutputBuffer;

    public Vector2Int[] zoneMoveInputs;
    public Vector4[] zoneDepthInputs;

    public ComputeBuffer zoneMoveInputBuffer;
    public ComputeBuffer zoneOutputBuffer;
    public ComputeBuffer zoneDepthInputBuffer;

    public int zoneComputeGroupSize;
    public int zoneKernelInit;
    public int zoneKernelUpdate;
}
