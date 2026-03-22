using System;
using UnityEngine;
using static Atlas;
public static class AtlasSpawn
{
    public const int ZONE_SPAWNER_COUNT = 8;

    public const int THREADS_PER_GROUP = 64;
    public const int FORE_PARTICLE_COUNT = 64;
    public const int MID_PARTICLE_COUNT = 256;
    public const int BACK_PARTICLE_COUNT = 128;

    public static readonly int MAX_VERTEX_COUNT = (FORE_PARTICLE_COUNT + MID_PARTICLE_COUNT + BACK_PARTICLE_COUNT) * 4;
    public static readonly int ZONE_STRIDE = FLOAT3_SIZE + (FLOAT_SIZE * 2) + (INT_SIZE * 2);

    //TODO Need to set buffer strings to ids if this buffer idea works
    public static string[] BUFFER_STRINGS = { "_Foreground0", "_Middleground0", "_Middleground1", "_Middleground2", "_Middleground3", "_Background0", "_Background1", "_Background2" };
    public static readonly string[] INIT_KERNEL_STRINGS = { "CSForeground0_Init", "CSMiddleground0_Init", "CSMiddleground1_Init", "CSMiddleground2_Init", "CSMiddleground3_Init", "CSBackground0_Init", "CSBackground1_Init", "CSBackground2_Init" };
    public static readonly string[] UPDATE_KERNEL_STRINGS = { "CSForeground0_Update", "CSMiddleground0_Update", "CSMiddleground1_Update", "CSMiddleground2_Update", "CSMiddleground3_Update", "CSBackground0_Update", "CSBackground1_Update", "CSBackground2_Update" };
    public static readonly string[] ACTIVE_STRINGS = { "_F0Active", "_M0Active", "_M1Active", "_M2Active", "_M3Active", "_B0Active", "_B1Active", "_B2Active" };
    public static readonly string[] DEAD_COUNT_STRINGS = { "_F0DeadCount", "_M0DeadCount", "_M1DeadCount", "_M2DeadCount", "_M3DeadCount", "_B0DeadCount", "_B1DeadCount", "_B2DeadCount" };
   
    public static readonly int[] PARTICLE_COUNTS = { FORE_PARTICLE_COUNT, MID_PARTICLE_COUNT, MID_PARTICLE_COUNT, MID_PARTICLE_COUNT, MID_PARTICLE_COUNT, BACK_PARTICLE_COUNT, BACK_PARTICLE_COUNT, BACK_PARTICLE_COUNT };

    public enum ZoneParticleType
    {
        None,
        TreesLOD0,
        TreesLOD1,
        HousesLOD0,
        HousesLOD1,
        ApartmentLOD0,
        ApartmentLOD1,
    }
    public enum ZoneArea
    {
        Foreground0,
        Middleground0,
        Middleground1,
        Middleground2,
        Middleground3,
        Background0,
        Background1,
        Background2,
    }
    [Serializable] public struct Zone
    {
        public AtlasSO atlas;
        public int metersStart;
        public int metersEnd;

        public Vector4[] zoneUVSizeAndPosArray;
        public Vector2[] zoneWorldSizesArray;
}
    [Serializable] public class ZoneSpawnerData
    {
        public Zone[] zones;
        public ZoneArea area;

        [Header("Generated")]
        public MaterialPropertyBlock mpb;
        public int particleCount;
        public int computeGroupSize;
        public int kernelID_init;
        public int kernelID_update;
        public bool active;
    }
}
