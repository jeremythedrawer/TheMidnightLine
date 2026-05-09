using System;
using System.Runtime.InteropServices;
using UnityEngine;
public static class AtlasSpawn
{
    public const float MAIN_MIN = 1.0f;
    public const float MAIN_MAX = 26.0f;

    public const int TRAIN_TRACKS_DEPTH = 6;
    public const int TRAIN_LINE_DEPTH = 5;
    public const float TRAIN_WHEEL_HEIGHT = -1.43f;
    public const float TRAIN_LINE_HEIGHT = 5.11f;

    public const float FAR_CLIP = 128;

    public const int SCROLL_PARTICLE_COUNT = 256;
    public const int ZONE_PARTICLE_COUNT = 2048;

    public static int PARTICLE_SPRITE_DATA_STRIDE = Marshal.SizeOf<ParticleSpriteData>();
    public const int MAX_PARTICLE_SPRITE_DATA_COUNT = 64;

    public const float THREADS_PER_GROUP = 64;

    public const int MAX_MPB_POOL_COUNT = 32;
    public static MaterialPropertyBlock[] mpb_pool;
    public static int mpbCount;
    public static uint[] argsSpawn = new uint[5] { 6, 0, 0, 0, 0 };
    public enum ParticleType
    { 
        Zone,
        Scroll,
    }


    public enum ParticleWidthType
    { 
        Simple,
        Sliced,
        Tiled,
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

    [Serializable] public struct ParticlePosData
    {
        public MaterialPropertyBlock mpb;
        public GraphicsBuffer argsBuffer;
        public int ticketCheckStart;
        public int ticketCheckEnd;

        public int particleCount;

        public ParticleWidthType widthType;

        public int depth;
        public int depthSize;
        public int spriteIndex;
        public float height;
    }

    [Serializable] public struct ParticleSpriteData
    {
        public Vector4 uvSizeAndPos;
        public Vector4 worldPivotAndSize;
        public Vector4 scaleAndFlip;
    }

    public static void InitMPBPool()
    {
        mpb_pool = new MaterialPropertyBlock[MAX_MPB_POOL_COUNT];
        mpbCount = -1;
    }

    public static MaterialPropertyBlock GetMPB()
    {
        MaterialPropertyBlock mpb_instance;
        if (mpbCount < 0)
        {
            mpb_instance = new MaterialPropertyBlock();
        }
        else
        {
            mpb_instance = mpb_pool[mpbCount];
            mpbCount--;
        }


        return mpb_instance;
    }
    public static void ReturnMPB(MaterialPropertyBlock mpb)
    {
        if (mpbCount == MAX_MPB_POOL_COUNT - 1) return;

        mpbCount++;
        
        mpb.Clear();
        mpb_pool[mpbCount] = mpb;
    }
}
