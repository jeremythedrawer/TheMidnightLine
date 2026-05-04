using System;
using System.Runtime.InteropServices;
using UnityEngine;
public static class AtlasSpawn
{
    public const float MAIN_MIN = 1.0f;
    public const float MAIN_MAX = 26.0f;
    public const float TRAIN_TRACKS_DEPTH = 6f;
    public const float TRAIN_LINE_DEPTH = 5f;
    public const float FORE_MIN = 1.0f;
    public const float FORE_SIZE = 1.0f;
    public const float MID_MIN = 48.0f;
    public const float MID_SIZE = 16.0f;
    public const float BACK_MIN = 65.0f;

    const float BACK_SIZE = 63.0f;
    public const float FAR_CLIP = 128;

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
    public enum ScrollSpriteType
    { 
        Simple,
        Sliced,
        Tiled,
    }

    public enum ZoneLabel
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
    public enum ZoneState
    { 
        None,
        Alive,
        Dying,
        Dead,
    }
    public enum ScrollState
    { 
        None,
        MovingIn,
        MovingOut,
        Dead,
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
        public Vector4 worldPivotAndSize;
        public Vector4 sliceOffsetAndSize;
    };

    [Serializable] public struct ZoneAtlas
    {
        public int ticketCheckStart;
        public int ticketCheckEnd;

        public AtlasSO atlas;
        
        public ZoneSpriteType zoneType;

        public Vector4[] uvSizeAndPosArray;
        public Vector4[] worldPivotAndSizeArray;
        public Vector4[] sliceOffsetsAndSizes;
    }

    [Serializable] public struct ScrollSprite
    {
        public int ticketCheckStart;
        public int ticketCheckEnd;

        public AtlasSO atlas;
        
        public ScrollSpriteType scrollType;
        
        public int depth;
        public int spriteIndex;
        public float height;
    }
}
