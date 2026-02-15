using System;
using System.Collections.Generic;
using UnityEngine;
using static Atlas;
public static class AtlasBatch
{
    public const int MAX = 1024;

    [Serializable]
    public struct BatchKey
    {
        public Material material;
        public Mesh mesh;
        public int depthOrder;
    }

    public class BatchData
    {
        public readonly List<AtlasRenderer> renderers = new List<AtlasRenderer>();
        public readonly Matrix4x4[] matrices = new Matrix4x4[MAX];
        public readonly Vector4[] uvSizeAndPosData = new Vector4[MAX];
        public readonly Vector4[] widthHeightArray = new Vector4[MAX];
        public readonly MaterialPropertyBlock mpb = new MaterialPropertyBlock();
    }

    public static readonly Dictionary<BatchKey, BatchData> batchDict = new Dictionary<BatchKey, BatchData>();
    public static readonly List<(BatchKey key, BatchData data)> batchList = new List<(BatchKey key, BatchData data)>();
    static Mesh quad;
    public static void PrepareFrame()
    {
        batchList.Clear();

        foreach (var kv in batchDict)
        {
            BatchKey key = kv.Key;
            BatchData batch = kv.Value;

            batchList.Add((key, batch));
        }
        batchList.Sort((a, b) => b.key.depthOrder.CompareTo(a.key.depthOrder));
    }
    public static void RegisterRenderer(AtlasRenderer atlasRenderer)
    {
        if (atlasRenderer.atlas == null || atlasRenderer.material == null) return;

        Material mat = atlasRenderer.material;
        
        if (!mat.enableInstancing) mat.enableInstancing = true;

        BatchKey key = new BatchKey
        {
            material = mat,
            mesh = GetQuad(),
            depthOrder = atlasRenderer.depthOrder,
        };

        if (!batchDict.TryGetValue(key, out BatchData batch))
        {
            batch = new BatchData();
            batchDict.Add(key, batch);
        }
        batch.renderers.Add(atlasRenderer);
        atlasRenderer.batchKey = key;

    }
    public static void UnregisterRenderer(AtlasRenderer atlasRenderer)
    {
        if (!batchDict.TryGetValue(atlasRenderer.batchKey, out BatchData batch)) return;

        batch.renderers.Remove(atlasRenderer);

        if (batch.renderers.Count == 0) batchDict.Remove(atlasRenderer.batchKey);
    }
    public static Mesh GetQuad()
    {
        if (quad != null) return quad;

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
        return quad;
    }
}
