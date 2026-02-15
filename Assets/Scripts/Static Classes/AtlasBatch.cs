using System;
using System.Collections.Generic;
using UnityEngine;
using static Atlas;
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
        public readonly Vector4[] widthHeightArray = new Vector4[MAX];
        public readonly Vector4[] flipArray = new Vector4[MAX];
        public readonly MaterialPropertyBlock mpb = new MaterialPropertyBlock();
    }

    public static readonly Dictionary<BatchKey, BatchData> batchDict = new Dictionary<BatchKey, BatchData>();
    public static readonly List<(BatchKey key, BatchData data)> batchList = new List<(BatchKey key, BatchData data)>();
    static Mesh quad;
    static Mesh quad9Slice;
    static readonly Dictionary<Vector4, Mesh> slicedQuads = new();
    public static void PrepareFrame()
    {
        batchList.Clear();
        foreach (var kv in batchDict) batchList.Add((kv.Key, kv.Value));
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

    public static Mesh Get9SliceQuad(Vector4 slice, Vector4 widthAndHeight, Vector2 atlasTextureSize)
    {
        quad9Slice = new Mesh();
        quad9Slice.name = "AtlasBatch9SliceQuad";

        float width = widthAndHeight.x;
        float height = widthAndHeight.y;

        float leftUV = slice.x;
        float rightUV = slice.y;
        float bottomUV = slice.z;
        float topUV = slice.w;

        float worldLeft = (leftUV * atlasTextureSize.x) / PIXELS_PER_UNIT;
        float worldRight = (rightUV * atlasTextureSize.x) / PIXELS_PER_UNIT;
        float worldBottom = (bottomUV * atlasTextureSize.y) / PIXELS_PER_UNIT;
        float worldTop = (topUV * atlasTextureSize.y) / PIXELS_PER_UNIT;
        float x0 = 0f;
        float x1 = x0 + worldLeft;
        float x2 = x1 + (width * (atlasTextureSize.x / PIXELS_PER_UNIT));
        float x3 = x2 + worldLeft;

        float y0 = 0f;
        float y1 = y0 + worldBottom;
        float y2 = y1 + (height * (atlasTextureSize.y / PIXELS_PER_UNIT));
        float y3 = y2 + worldBottom;

        quad9Slice.vertices = new Vector3[]
        {
            new Vector3(x0, y0),
            new Vector3(x1, y0),
            new Vector3(x2, y0),
            new Vector3(x3, y0),

            new Vector3(x0, y1),
            new Vector3(x1, y1),
            new Vector3(x2, y1),
            new Vector3(x3, y1),

            new Vector3(x0, y2),
            new Vector3(x1, y2),
            new Vector3(x2, y2),
            new Vector3(x3, y2),

            new Vector3(x0, y3),
            new Vector3(x1, y3),
            new Vector3(x2, y3),
            new Vector3(x3, y3),

        };

        quad9Slice.uv = new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(leftUV, 0),
            new Vector2(rightUV, 0),
            new Vector2(1, 0),

            new Vector2(0, bottomUV),
            new Vector2(leftUV, bottomUV),
            new Vector2(rightUV, bottomUV),
            new Vector2(1, bottomUV),

            new Vector2(0, topUV),
            new Vector2(leftUV, topUV),
            new Vector2(rightUV, topUV),
            new Vector2(1, topUV),

            new Vector2(0, 1),
            new Vector2(leftUV, 1),
            new Vector2(rightUV, 1),
            new Vector2(1, 1),
        };

        quad9Slice.triangles = new int[]
        {
            0,4,5, 0,5,1,
            1,5,6, 1,6,2,
            2,6,7, 2,7,3,

            4,8,9, 4,9,5,
            5,9,10, 5,10,6,
            6,10,11, 6,11,7,

            8,12,13, 8,13,9,
            9,13,14, 9,14,10,
            10,14,15, 10,15,11
        };
        quad9Slice.RecalculateBounds();
        return quad9Slice;
    }
}
