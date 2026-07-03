using System;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using static AtlasRendering;
public static class AtlasSpawn
{
    public const float MAIN_MIN = 3.0f;
    public const float MAIN_MAX = 26.0f;

    public const int TRAIN_TRACKS_DEPTH = 6;
    public const int TRAIN_LINE_DEPTH = 5;
    public const float TRAIN_WHEEL_HEIGHT = -0.903f;
    public const float TRAIN_LINE_HEIGHT = 2.353f;

    public const float FAR_CLIP = 128;

    public const int SCROLL_PARTICLE_COUNT = 256;
    public const int ZONE_PARTICLE_COUNT = 1024;

    public const int MAX_QUAD_COUNT = 16384;

    public const int MAX_EDGE_SPRITE_COUNT = 16;

    public const int MAX_POS_DATA_AT_ONCE_COUNT = 32;
    public const int MAX_EDGE_SPRITE_DATA_AT_ONCE_COUNT = 32;
    public const int MAX_PARTICLE_SPRITE_DATA_COUNT = 128;
    public const float THREADS_PER_GROUP = 64;

    public static int PARTICLE_SPRITE_DATA_STRIDE = Marshal.SizeOf<ParticleSpritesData>();
    public static int EDGE_SPRITE_DATA_STRIDE = Marshal.SizeOf<EdgeSpriteData>();

    public static MaterialPropertyBlock[] mpb_pool;
    public static int mpbPoolCount;

    public static GraphicsBuffer[] argsBuffer_pool;
    public static int argsBufferPoolCount;

    public static GraphicsBuffer[] quadScaleBuffer_pool;
    public static int quadScaleBufferPoolCount;

    public static GraphicsBuffer[] edgeSpriteDataBuffer_pool;
    public static int edgeSpriteDataBufferPoolCount;

    public static uint[] argsSpawn = new uint[5] { 6, 0, 0, 0, 0 };


    [Flags] public enum ParticleMoveInputs
    {
        None = 0,
        Born = 1 << 0,
        Dying = 1 << 1,
        Dead = 1 << 2,
        FirstOutOfBounds = 1 << 3,
        Elevation = 1 << 4,

    }
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
    public enum SpawnState
    { 
        None,
        MovingIn,
        MovingOut,
        Alive,
    }

    [Serializable] public struct EdgeSpriteData
    {
        public uint spriteIndex;
        public Vector4 offset;
        public Vector4 scale;
    }
    [Serializable] public struct EdgeScroller
    {
        public EdgeSpriteData[] spriteData;
        public MaterialPropertyBlock mpb;
        public GraphicsBuffer argsBuffer;
        public GraphicsBuffer edgeSpriteDataBuffer;
    }

    [Serializable] public struct ParticlePosData
    {
        public EdgeScroller[] preScrollers;
        public EdgeScroller[] postScrollers;

        public string prefabGUID;
        public Vector4[] quadScales;

        public Vector2Int prevDepthIndices;
        
        public MaterialPropertyBlock mpb;
        public GraphicsBuffer argsBuffer;
        public GraphicsBuffer quadScaleBuffer;

        public ParticleWidthType widthType;
        public SpawnState spawnState;

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
        public float posX;
        public float scaleY;
        public float scaleX;
        public float randScale;

        public bool elevate;
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
        public ComputeBuffer offsetInputBuffer;
        public ComputeBuffer prevIndicesInputsBuffer;

        public uint[] moveInputs;
        public Vector4[] depthInputs;
        public Vector4[] offsetInputs;
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
    public static void InitEdgeSpritePool()
    {
        edgeSpriteDataBuffer_pool = new GraphicsBuffer[MAX_EDGE_SPRITE_DATA_AT_ONCE_COUNT];
        edgeSpriteDataBufferPoolCount = -1;
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

    public static GraphicsBuffer GetEdgeSpriteBuffer()
    {
        GraphicsBuffer edgeSpriteBuffer_instance;

        if (edgeSpriteDataBufferPoolCount < 0)
        {
            edgeSpriteBuffer_instance = new GraphicsBuffer(GraphicsBuffer.Target.Structured, MAX_EDGE_SPRITE_COUNT, EDGE_SPRITE_DATA_STRIDE);
        }
        else
        {
            edgeSpriteBuffer_instance = edgeSpriteDataBuffer_pool[edgeSpriteDataBufferPoolCount];
            edgeSpriteDataBufferPoolCount--;
        }
        return edgeSpriteBuffer_instance;
    }

    public static void ReturnEdgeSpriteBuffer(GraphicsBuffer edgeSpriteBuffer)
    {
        if (edgeSpriteDataBufferPoolCount == MAX_EDGE_SPRITE_DATA_AT_ONCE_COUNT - 1) return;
        edgeSpriteDataBufferPoolCount++;
        edgeSpriteDataBuffer_pool[edgeSpriteDataBufferPoolCount] = edgeSpriteBuffer;
    }

    public static void Dispose()
    {
        for (int i = 0; i < argsBufferPoolCount; i++)
        {
            argsBuffer_pool[i]?.Release();
        }
        for (int i = 0; i < quadScaleBufferPoolCount; i++)
        {
            quadScaleBuffer_pool[i]?.Release();
        }
        for(int i = 0; i < edgeSpriteDataBufferPoolCount; i++)
        {
            edgeSpriteDataBuffer_pool[i]?.Release();
        }
    }
}
#if UNITY_EDITOR

[InitializeOnLoad]
public static class AtlasSpawnCleanup
{
    static AtlasSpawnCleanup()
    {
        AssemblyReloadEvents.beforeAssemblyReload += AtlasSpawn.Dispose;
    }
}
#endif