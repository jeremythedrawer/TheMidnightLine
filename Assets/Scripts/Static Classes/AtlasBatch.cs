using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
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

    [Serializable] public class RenderInput
    {
        public BatchKey batchKey;

        [Header("Generated")]
        public GameObject gameObject;
        public Vector3[] position = new Vector3[1];
        public Vector2[] pivot = new Vector2[1];
        public Vector4[] uvSizeAndPos = new Vector4[1];
        public Vector4[] widthHeightFlip = new Vector4[1];
        public SpriteMode spriteMode;
    }

    [Serializable] public struct SpriteData
    {
        public Vector4 position;
        public Vector4 pivot;
        public Vector4 uvSizeAndPos;
        public Vector4 widthHeightFlip;
    }
    public class BatchData
    {
        public List<RenderInput> renderInputs = new List<RenderInput>();
        public readonly SpriteData[] spriteData = new SpriteData[MAX];
        
        public GraphicsBuffer spriteDataBuffer;
        public GraphicsBuffer argsBuffer;

        public readonly MaterialPropertyBlock mpb = new MaterialPropertyBlock();
    }

    public class UIBatchData
    {
        public readonly List<RenderInput> uiData = new List<RenderInput>();
        public readonly Matrix4x4[] matrices = new Matrix4x4[MAX];
        public readonly Vector4[] uvSizeAndPosData = new Vector4[MAX];
        public readonly Vector4[] widthHeightFlip = new Vector4[MAX];
        public readonly MaterialPropertyBlock mpb = new MaterialPropertyBlock();
    }


    public static readonly Dictionary<BatchKey, BatchData> batchDict = new Dictionary<BatchKey, BatchData>();
    public static readonly List<(BatchKey key, BatchData)> batchList = new List<(BatchKey key, BatchData data)>();

    public static readonly Dictionary<BatchKey, UIBatchData> textBatchDict = new Dictionary<BatchKey, UIBatchData>();
    public static readonly List<(BatchKey key, UIBatchData)> uiBatchList = new List<(BatchKey key, UIBatchData)>();
    public static Mesh quad;
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
    public static void RegisterRenderer(RenderInput renderInput)
    {
        if (renderInput.batchKey.material == null) return;
        renderInput.batchKey.material.enableInstancing = true;
        UnregisterRenderer(renderInput);

        if (!batchDict.TryGetValue(renderInput.batchKey, out BatchData batch))
        {
            batch = new BatchData();
            batch.spriteDataBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, MAX, SHADER_DATA_STRIDE);
            batch.argsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, ARGS_STRIDE);
            batchDict.Add(renderInput.batchKey, batch);
        }

        batch.renderInputs.Add(renderInput);

    }
    public static void UnregisterRenderer(RenderInput renderInput)
    {
        if (!batchDict.TryGetValue(renderInput.batchKey, out BatchData batch)) return;

        batch.renderInputs.Remove(renderInput);

        if (batch.renderInputs.Count == 0) batchDict.Remove(renderInput.batchKey);
    }
    public static void RegisterUIRenderer(RenderInput uiRenderInstance)
    {
        if (uiRenderInstance.batchKey.material == null) return;
        UnregisterUIRenderer(uiRenderInstance);

        if (!uiRenderInstance.batchKey.material.enableInstancing) uiRenderInstance.batchKey.material.enableInstancing = true;

        if (!textBatchDict.TryGetValue(uiRenderInstance.batchKey , out UIBatchData batch))
        {
            batch = new UIBatchData();
            textBatchDict.Add(uiRenderInstance.batchKey, batch);
        }

        batch.uiData.Add(uiRenderInstance);
    }
    public static void UnregisterUIRenderer(RenderInput uiRenderInstance)
    {
        if (!textBatchDict.TryGetValue(uiRenderInstance.batchKey, out UIBatchData batch)) return;

        batch.uiData.Remove(uiRenderInstance);

        if (batch.uiData.Count == 0) textBatchDict.Remove(uiRenderInstance.batchKey);
    }
    public static void SetQuad()
    {
        quad = new Mesh();
        quad.name = "AtlasBatchQuad";

        quad.vertices = new Vector3[]
        {
            new Vector3(0f, 0f),
            new Vector3(1f, 0f),
            new Vector3(0f,  1f),
            new Vector3(1f,  1f),
        };

        quad.uv = new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1),
        };

        quad.triangles = new int[] { 0, 2, 1, 2, 3, 1 };
        quad.RecalculateBounds();

        args = new uint[5]
        {
            AtlasBatch.quad.GetIndexCount(0),
            0,
            AtlasBatch.quad.GetIndexStart(0),
            AtlasBatch.quad.GetBaseVertex(0),
            0
        };
    }
}
