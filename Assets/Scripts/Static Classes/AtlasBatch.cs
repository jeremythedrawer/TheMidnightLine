using System;
using System.Collections.Generic;
using UnityEngine;
public static class AtlasBatch
{
    public const int MAX = 1024;
    [Serializable] public struct BatchKey
    {
        public Material material;
        public Mesh mesh;
    }

    public class BatchData
    {
        public readonly List<AtlasRenderer> renderers = new List<AtlasRenderer>();
        public readonly Matrix4x4[] matrices = new Matrix4x4[MAX];
        public readonly Vector4[] uvSizeAndPosData = new Vector4[MAX];
        public readonly MaterialPropertyBlock mpb = new MaterialPropertyBlock();
    }

    public static readonly Dictionary<BatchKey, BatchData> batchDict = new Dictionary<BatchKey, BatchData>();
    public static readonly List<(BatchKey key, BatchData data)> batchList = new List<(BatchKey key, BatchData data)>();
    static Mesh quad;

    public static void PrepareFrame()
    {
        batchList.Clear();
        foreach (var kv in batchDict) batchList.Add((kv.Key, kv.Value));
    }
    public static void RegisterRenderer(AtlasRenderer atlasRenderer)
    {
        Material mat = atlasRenderer.atlas.material;

        if (!mat.enableInstancing) mat.enableInstancing = true;

        BatchKey key = new BatchKey
        {
            material = mat,
            mesh = GetQuad()
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
    }
    static Mesh GetQuad()
    {
        if (quad != null) return quad;

        quad = new Mesh();
        quad.name = "AtlasBatchQuad";

        quad.vertices = new Vector3[]
        {
            new Vector3(-0.5f, -0.5f, 0),
            new Vector3( 0.5f, -0.5f, 0),
            new Vector3(-0.5f,  0.5f, 0),
            new Vector3( 0.5f,  0.5f, 0),
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

    static Material GetMaterial(Material src)
    {
        if (!src.enableInstancing) src.enableInstancing = true;
        return src;
    }
}
