using System;
using System.Runtime.InteropServices;
using UnityEngine;
using static AtlasRendering;
public static class AtlasSpawn
{
    public const float MAIN_MIN = 1.0f;
    public const float MAIN_MAX = 26.0f;

    public const int TRAIN_TRACKS_DEPTH = 6;
    public const int TRAIN_LINE_DEPTH = 5;
    public const float TRAIN_WHEEL_HEIGHT = -0.903f;
    public const float TRAIN_LINE_HEIGHT = 2.353f;

    public const float FAR_CLIP = 128;

    public const int SCROLL_PARTICLE_COUNT = 256;
    public const int ZONE_PARTICLE_COUNT = 1024;

    public const int MAX_QUAD_COUNT = 16384;
    
    public const int MAX_POS_DATA_AT_ONCE_COUNT = 32;
    public const int MAX_PARTICLE_SPRITE_DATA_COUNT = 128;
    public const float THREADS_PER_GROUP = 64;

    public static int PARTICLE_SPRITE_DATA_STRIDE = Marshal.SizeOf<ParticleSpritesData>();

    public static MaterialPropertyBlock[] mpb_pool;
    public static int mpbPoolCount;

    public static GraphicsBuffer[] argsBuffer_pool;
    public static int argsBufferPoolCount;

    public static GraphicsBuffer[] quadScaleBuffer_pool;
    public static int quadScaleBufferPoolCount;

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

        public Vector2Int prevDepthIndices;
        public MaterialPropertyBlock mpb;
        public GraphicsBuffer argsBuffer;

        public Vector4[] quadScales;
        public GraphicsBuffer quadScaleBuffer;

        public ParticleWidthType widthType;

        public int ticketCheckStart;
        public int ticketCheckEnd;

        public uint quadCount;

        public int particleCount;
        public int minParticleIndex;
        public int maxParticleIndex;

        public int depth;
        public int depthSize;
        
        public int spriteIndex;
        public int spritesPerParticle;

        public float posY;
        public float scaleY;
        public float scaleX;
        public float randScale;

        public bool isDying;
        public bool isAlive;
    }

    [Serializable] public struct ParticleSpritesData
    {
        public Vector4 uvSizeAndPos;
        public Vector4 worldPivotAndSize;
    }

    [Serializable] public struct SpawnComputeData
    {
        public ParticleType particleType;
        public ComputeShader compute;
        public Material material;

        [Header("Generated")]
        public ComputeBuffer moveInputBuffer;
        public ComputeBuffer outputBuffer;
        public ComputeBuffer depthInputBuffer;
        public ComputeBuffer heightInputBuffer;
        public ComputeBuffer prevIndicesInputsBuffer;

        public Vector2Int[] moveInputs;
        public Vector4[] depthInputs;
        public float[] heightInputs;
        public Vector2Int[] prevIndicesInputs;

        public int initKernel;
        public int updateKernel;
        public int groupSize;
    }

    [Serializable] public struct DelayedParticleData
    {
        public ParticlePosData posData;
        public ParticleAtlas particleAtlas;
        public int index;
        public SpawnComputeData spawnComputeData;
    }

    public static void InitMPBPool()
    {
        mpb_pool = new MaterialPropertyBlock[MAX_POS_DATA_AT_ONCE_COUNT];
        mpbPoolCount = -1;
    }

    public static void InitArgsPool()
    {
        argsBuffer_pool = new GraphicsBuffer[MAX_POS_DATA_AT_ONCE_COUNT];
        argsBufferPoolCount = -1;
    }

    public static void InitQuadScalePool()
    {
        quadScaleBuffer_pool = new GraphicsBuffer[MAX_PARTICLE_SPRITE_DATA_COUNT];
        quadScaleBufferPoolCount = -1;
    }
    public static MaterialPropertyBlock GetMPB()
    {
        MaterialPropertyBlock mpb_instance;
        if (mpbPoolCount < 0)
        {
            mpb_instance = new MaterialPropertyBlock();
        }
        else
        {
            mpb_instance = mpb_pool[mpbPoolCount];
            mpbPoolCount--;
        }


        return mpb_instance;
    }
    public static void ReturnMPB(MaterialPropertyBlock mpb)
    {
        if (mpbPoolCount == MAX_POS_DATA_AT_ONCE_COUNT - 1) return;

        mpbPoolCount++;
        
        mpb.Clear();
        mpb_pool[mpbPoolCount] = mpb;
    }

    public static GraphicsBuffer GetArgsBuffer()
    {
        GraphicsBuffer argsBuffer_instance;

        if (argsBufferPoolCount < 0)
        {
            argsBuffer_instance = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, ARGS_STRIDE);

        }
        else
        {
            argsBuffer_instance = argsBuffer_pool[argsBufferPoolCount];
            argsBufferPoolCount--;
        }

        return argsBuffer_instance;
    }

    public static void ReturnArgsBuffer(GraphicsBuffer args)
    {
        if (argsBufferPoolCount == MAX_POS_DATA_AT_ONCE_COUNT - 1) return;

        argsBufferPoolCount++;

        argsBuffer_pool[argsBufferPoolCount] = args;
    }

    public static GraphicsBuffer GetQuadScaleBuffer()
    {
        GraphicsBuffer quadScaleBuffer_instance;

        if (quadScaleBufferPoolCount < 0)
        {
            quadScaleBuffer_instance = new GraphicsBuffer(GraphicsBuffer.Target.Structured, MAX_QUAD_COUNT, sizeof(float) * 4);
        }
        else
        {
            quadScaleBuffer_instance = quadScaleBuffer_pool[quadScaleBufferPoolCount];
            quadScaleBufferPoolCount--;
        }
        return quadScaleBuffer_instance;
    }

    public static void ReturnQuadScaleBuffer(GraphicsBuffer quadScaleBuffer)
    {
        if (quadScaleBufferPoolCount == MAX_POS_DATA_AT_ONCE_COUNT - 1) return;

        quadScaleBufferPoolCount++;

        quadScaleBuffer_pool[quadScaleBufferPoolCount] = quadScaleBuffer;
    }
}
