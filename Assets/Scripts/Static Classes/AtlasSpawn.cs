using System;
using System.Runtime.InteropServices;
using UnityEngine;
using static Atlas;
using static AtlasRendering;
public static class AtlasSpawn
{
    public const int ZONE_SPAWNER_COUNT = 8;

    public const int THREADS_PER_GROUP = 64;
    public const int FORE_PARTICLE_COUNT = 16;
    public const int MID_PARTICLE_COUNT = 128;
    public const int BACK_PARTICLE_COUNT = 256;

    public static readonly int MAX_VERTEX_COUNT = (FORE_PARTICLE_COUNT + MID_PARTICLE_COUNT + BACK_PARTICLE_COUNT) * 4;
    public static readonly int ZONE_OUTPUT_STRIDE = Marshal.SizeOf(typeof(ZoneOutput));
    public static readonly int ZONE_INPUT_STRIDE = Marshal.SizeOf(typeof(ZoneInput));

    //TODO Need to set buffer strings to ids if this buffer idea works
    public static string[] ZONE_STRINGS = { "_F0", "_M0", "_M1", "_M2", "_M3", "_B0", "_B1", "_B2" };
    public static readonly string ACTIVE_STRING = "Active";
    public static readonly string DEAD_COUNT_STRING = "DeadCount";
    public static readonly string OUTPUT_STRING = "Output";
    public static readonly string INPUT_STRING = "Input";
    public static readonly string SPRITE_COUNT_STRING = "SpriteCount";
    public static readonly string INIT_STRING = "Init";
    public static readonly string INIT_SLICE_STRING = "InitSlice";
    public static readonly string UPDATE_STRING = "Update";
    public static readonly int[] PARTICLE_COUNTS = { FORE_PARTICLE_COUNT, MID_PARTICLE_COUNT, MID_PARTICLE_COUNT, MID_PARTICLE_COUNT, MID_PARTICLE_COUNT, BACK_PARTICLE_COUNT, BACK_PARTICLE_COUNT, BACK_PARTICLE_COUNT };

    public enum ZoneSpriteType
    {
        Simple,
        Sliced,
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

    struct ZoneOutput
    {
        Vector4 uvSizeAndPos;
        Vector4 position;
        Vector4 scale;
        Vector4 worldPivotAndSize;
        float parallaxFactor;
        float rand01;
        uint alive;
        uint randID;
    };

    [Serializable] public struct ZoneInput
    {
        public Vector4 uvSizeAndPos;
        public Vector4 worldSizeAndPivot;
        public Vector4 sliceOffsetAndSize;
    };

    [Serializable] public struct Zone // NOTE: Turn into Scriptable Object
    {
        public AtlasSO atlas;
        public int ticketCheckStart;
        public int ticketCheckEnd;

        public Vector4[] zoneUVSizeAndPosArray;
        public Vector4[] zoneWorldPivotsAndSizesArray;
        public Vector4[] zoneSliceOffsetsAndSizes;
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
        public int kernelID_initSlice;
        public int kernelID_update;
        public bool active;
    }
}
