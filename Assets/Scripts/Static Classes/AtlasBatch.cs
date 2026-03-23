using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UIElements;
using static Atlas;
public static class AtlasBatch
{
    static AtlasBatch() { SetQuad(); }

    public const int MAX = 1024;
    public static int SHADER_DATA_STRIDE = Marshal.SizeOf<SpriteData>();
    public static int ARGS_STRIDE = INT_SIZE * 5;
    [Serializable] public struct BatchKey
    {
        public Material material;
        public int depthOrder;
    }

    [Serializable] public class SingularRenderInput
    {
        public BatchKey batchKey;
        public AtlasSO atlas;
        public BoxCollider2D boxCollider;

        public bool flipX;
        public bool flipY;
        [Header("Generated")]
        public GameObject gameObject;
        public MaterialPropertyBlock customMPB;
        public Bounds bounds = new Bounds();

        public Vector4 pivotAndSize;
        public Vector4 uvSizeAndPos;
        public Vector4 scaleAndFlip;
        public Vector2 boundsOffset;


        public void UpdateRenderInputsScreen(float width, float height, SimpleSprite newSprite, CameraStatsSO camStats, Transform transform)
        {
            if (atlas == null) return;
            Vector2 spritePixelSize = newSprite.worldSize * PIXELS_PER_UNIT;
            uvSizeAndPos = newSprite.uvSizeAndPos;
            pivotAndSize.z = spritePixelSize.x * camStats.worldUnitsPerPixel;
            pivotAndSize.w = spritePixelSize.y * camStats.worldUnitsPerPixel;
            scaleAndFlip.x = width;
            scaleAndFlip.y = height;

        }
        public void UpdateRenderInputsWorld(float width, float height, SimpleSprite newSprite)
        {
            if (atlas == null) return;

            uvSizeAndPos = newSprite.uvSizeAndPos;
            pivotAndSize.z = newSprite.worldSize.x;
            pivotAndSize.w = newSprite.worldSize.y;
            scaleAndFlip.x = width;
            scaleAndFlip.y = height;
            bounds.size = new Vector3(scaleAndFlip.x * pivotAndSize.z, scaleAndFlip.y * pivotAndSize.w, 0.2f);

            FlipH(flipX, newSprite);
            FlipV(flipY, newSprite);

            bounds.center = new Vector3(gameObject.transform.position.x + boundsOffset.x, gameObject.transform.position.y + boundsOffset.y, gameObject.transform.position.z);

            if (boxCollider == null) return;
            boxCollider.size = bounds.size;
            boxCollider.offset = bounds.center - gameObject.transform.position;
        }
        public void UpdateDepth(int newDepth)
        {
            batchKey.depthOrder = newDepth;
            gameObject.transform.position = new Vector3(gameObject.transform.position.x, gameObject.transform.position.y, batchKey.depthOrder);
        }
        public void FlipH(bool flipLeft, SimpleSprite sprite)
        {
            flipX = flipLeft;
            scaleAndFlip.z = flipLeft ? -1 : 1;
            float flipPivot = flipLeft ? 1 - sprite.uvPivot.x : sprite.uvPivot.x;
            pivotAndSize.x = flipPivot * scaleAndFlip.x * pivotAndSize.z;
            boundsOffset.x = (bounds.size.x * 0.5f) - pivotAndSize.x;
        }
        public void FlipV(bool flipDown, SimpleSprite sprite)
        {
            flipY = flipDown;
            scaleAndFlip.w = flipDown ? -1 : 1;
            float flipPivot = flipDown ? 1 - sprite.uvPivot.y : sprite.uvPivot.y;
            pivotAndSize.y = flipPivot * scaleAndFlip.y * pivotAndSize.w;
            boundsOffset.y = (bounds.size.y * 0.5f) - pivotAndSize.y;
        }
        public void InitRenderer(GameObject obj)
        {
            gameObject = obj;
        }

        public void UpdateBounds()
        {
            if (gameObject.transform.hasChanged)
            {
                bounds.center = new Vector3(gameObject.transform.position.x + boundsOffset.x, gameObject.transform.position.y + boundsOffset.y, gameObject.transform.position.z);
            }
        }
    }

    [Serializable] public class MultipleRenderInput
    {
        public BatchKey batchKey;

        [Header("Generated")]
        public GameObject gameObject;
        public Bounds bounds = new Bounds();

        public Vector4[] worldPivotAndSize;
        public Vector4[] uvSizeAndPos;
        public Vector4[] scaleAndFlip;
    }

    [Serializable] public struct SpriteData
    {
        public Vector4 worldPosition;
        public Vector4 worldPivotAndScale;
        public Vector4 uvSizeAndPos;
        public Vector4 scaleAndFlip;
    }
    public class BatchData
    {
        public List<SingularRenderInput> singularRenderInputs = new List<SingularRenderInput>();
        public List<MultipleRenderInput> multipleRenderInputs = new List<MultipleRenderInput>();
        public readonly SpriteData[] spriteData = new SpriteData[MAX];
        
        public GraphicsBuffer spriteDataBuffer;
        public GraphicsBuffer argsBuffer;

        public readonly MaterialPropertyBlock mpb = new MaterialPropertyBlock();
    }

    public class UIBatchData
    {
        public readonly List<SingularRenderInput> uiData = new List<SingularRenderInput>();
        public readonly Matrix4x4[] matrices = new Matrix4x4[MAX];
        public readonly Vector4[] uvSizeAndPosData = new Vector4[MAX];
        public readonly Vector4[] widthHeightFlip = new Vector4[MAX];
        public readonly MaterialPropertyBlock mpb = new MaterialPropertyBlock();
    }


    public static readonly Dictionary<BatchKey, BatchData> batchDict = new Dictionary<BatchKey, BatchData>();
    public static readonly List<(BatchKey key, BatchData)> batchList = new List<(BatchKey key, BatchData data)>();

    public static readonly Dictionary<BatchKey, UIBatchData> textBatchDict = new Dictionary<BatchKey, UIBatchData>();
    public static readonly List<(BatchKey key, UIBatchData)> uiBatchList = new List<(BatchKey key, UIBatchData)>();
    public static Mesh Quad;
    public static uint[] args;


    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        SetQuad();
    }

    public static void PrepareFrame()
    {
        batchList.Clear();
        uiBatchList.Clear();

        foreach (var kv in batchDict)
        {
            BatchKey key = kv.Key;
            BatchData batch = kv.Value;
            batchList.Add((key, batch));
        }

        foreach (var kv in textBatchDict)
        {
            BatchKey key = kv.Key;
            UIBatchData batch = kv.Value;
            uiBatchList.Add((key, batch));
        }
    }
    public static void RegisterSingleRenderInput(SingularRenderInput renderInput)
    {
        if (renderInput.batchKey.material == null) return;
        renderInput.batchKey.material.enableInstancing = true;
        UnregisterSingleRenderInput(renderInput);

        if (!batchDict.TryGetValue(renderInput.batchKey, out BatchData batch))
        {
            batch = new BatchData();
            batch.spriteDataBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, MAX, SHADER_DATA_STRIDE);
            batch.argsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, ARGS_STRIDE);
            batchDict.Add(renderInput.batchKey, batch);
        }

        batch.singularRenderInputs.Add(renderInput);
    }
    public static void UnregisterSingleRenderInput(SingularRenderInput renderInput)
    {
        if (!batchDict.TryGetValue(renderInput.batchKey, out BatchData batch)) return;

        batch.singularRenderInputs.Remove(renderInput);

        if (batch.singularRenderInputs.Count == 0 && batch.multipleRenderInputs.Count == 0) batchDict.Remove(renderInput.batchKey);
    }


    public static void RegisterMultipleRenderInput(MultipleRenderInput renderInput)
    {
        if (renderInput.batchKey.material == null) return;
        renderInput.batchKey.material.enableInstancing = true;
        UnregisterMultipleRenderInput(renderInput);

        if (!batchDict.TryGetValue(renderInput.batchKey, out BatchData batch))
        {
            batch = new BatchData();
            batch.spriteDataBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, MAX, SHADER_DATA_STRIDE);
            batch.argsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, ARGS_STRIDE);
            batchDict.Add(renderInput.batchKey, batch);
        }

        batch.multipleRenderInputs.Add(renderInput);

    }
    public static void UnregisterMultipleRenderInput(MultipleRenderInput renderInput)
    {
        if (!batchDict.TryGetValue(renderInput.batchKey, out BatchData batch)) return;

        batch.multipleRenderInputs.Remove(renderInput);

        if (batch.singularRenderInputs.Count == 0 && batch.multipleRenderInputs.Count == 0) batchDict.Remove(renderInput.batchKey);
    }

    public static void SetQuad()
    {
        Quad = new Mesh();
        Quad.name = "AtlasBatchQuad";

        Quad.vertices = new Vector3[]
        {
            new Vector3(0f, 0f),
            new Vector3(1f, 0f),
            new Vector3(0f,  1f),
            new Vector3(1f,  1f),
        };

        Quad.uv = new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1),
        };

        Quad.triangles = new int[] { 0, 2, 1, 2, 3, 1 };
        Quad.RecalculateBounds();

        args = new uint[5]
        {
            AtlasBatch.Quad.GetIndexCount(0),
            0,
            AtlasBatch.Quad.GetIndexStart(0),
            AtlasBatch.Quad.GetBaseVertex(0),
            0
        };
    }
}
