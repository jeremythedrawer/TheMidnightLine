using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using static Atlas;
public static class AtlasRendering
{
    public const int MAX = 1024;
    public static int SHADER_DATA_STRIDE = Marshal.SizeOf<SpriteData>();
    public static int ARGS_STRIDE = INT_SIZE * 5;

    public enum AtlasRendererType
    { 
        SimpleWorld,
        MotionWorld,

        SliceWorld,
        TextWorld,

        SimpleScreen,
        MotionScreen,

        SliceScreen,
        TextScreen,
    }

    [Serializable] public struct BatchKey
    {
        public Material material;
        public int depthOrder;
    }
    [Serializable] public struct SpriteData
    {
        public Vector4 worldPosition;
        public Vector4 worldPivotAndScale;
        public Vector4 uvSizeAndPos;
        public Vector4 scaleAndFlip;
        public Vector4 custom;
    }

    public static readonly Dictionary<BatchKey, SpriteBatch> spriteBatchDict = new Dictionary<BatchKey, SpriteBatch>();
    public static readonly List<(BatchKey, SpriteBatch)> spriteBatchList = new List<(BatchKey, SpriteBatch)> ();

    public class SpriteBatch
    {
        public readonly List<AtlasRenderer> rendererList = new List<AtlasRenderer>();
        public readonly SpriteData[] spriteData = new SpriteData[MAX];

        public GraphicsBuffer spriteDataBuffer;
        public GraphicsBuffer argsBuffer;

        public readonly MaterialPropertyBlock mpb = new MaterialPropertyBlock();
    }
    public static void PrepareFrame()
    {
        spriteBatchList.Clear();

        foreach (KeyValuePair<BatchKey, SpriteBatch> kv in spriteBatchDict)
        {
            BatchKey key = kv.Key;
            SpriteBatch batch = kv.Value;
            spriteBatchList.Add((key, batch));
        }
    }

    public static void RegisterRenderer(AtlasRenderer renderer)
    {
        if (renderer.batchKey.material == null) return;
        renderer.batchKey.material.enableInstancing = true;

        UnregisterRenderer(renderer);

        renderer.batchKey.depthOrder = (int)renderer.transform.position.z;

        if (!spriteBatchDict.TryGetValue(renderer.batchKey, out SpriteBatch batch))
        {
            batch = new SpriteBatch();
            batch.spriteDataBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, MAX, SHADER_DATA_STRIDE);
            batch.argsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, ARGS_STRIDE);
            spriteBatchDict.Add(renderer.batchKey, batch);
        }

        batch.rendererList.Add(renderer);
    }

    public static void UnregisterRenderer(AtlasRenderer renderer)
    {
        if (!spriteBatchDict.TryGetValue(renderer.batchKey, out SpriteBatch batch)) return;

        batch.rendererList.Remove(renderer);
        if (batch.rendererList.Count == 0) spriteBatchDict.Remove(renderer.batchKey);
    }
    public static ref SimpleSprite GetNextKeyframeSprite(ref AtlasClip clip, ref float keyframeClock, ref int curFrameIndex, ref int prevFrameIndex)
    {
        keyframeClock += Time.deltaTime;

        float frameTime = keyframeClock * FRAMES_PER_SEC;
        if (curFrameIndex >= clip.keyFrames.Length || curFrameIndex < 0) curFrameIndex = 0;
        AtlasKeyframe curKeyFrame = clip.keyFrames[curFrameIndex];

        switch (clip.clipType)
        {
            case ClipType.Loop:
            {
                if (frameTime >= curKeyFrame.holdTime)
                {
                    prevFrameIndex = curFrameIndex;
                    curFrameIndex++;

                    if (curFrameIndex >= clip.keyFrames.Length)
                    {
                        curFrameIndex = 0;
                    }

                    keyframeClock = 0;
                }
            }
            break;
            case ClipType.PingPong:
            {
                if (frameTime >= curKeyFrame.holdTime)
                {
                    if (curFrameIndex < clip.keyFrames.Length - 1 && (curFrameIndex > prevFrameIndex || curFrameIndex == 0))
                    {
                        prevFrameIndex = curFrameIndex;
                        curFrameIndex++;
                    }
                    else
                    {
                        prevFrameIndex = curFrameIndex;
                        curFrameIndex--;
                    }
                    keyframeClock = 0;
                }
            }
            break;
            case ClipType.OneShot:
            {
                if (frameTime >= curKeyFrame.holdTime)
                {
                    prevFrameIndex = curFrameIndex;
                    if (curFrameIndex < clip.keyFrames.Length - 1)
                    {
                        curFrameIndex++;
                    }
                    keyframeClock = 0;
                }
            }
            break;
        }

        return ref clip.keyFrames[curFrameIndex].motionSprite.sprite;
    }
    public static ref SimpleSprite GetNextKeyframeSpriteReverse(ref AtlasClip clip, ref float keyframeClock, ref int curFrameIndex, ref int prevFrameIndex)
    {
        keyframeClock += Time.deltaTime;

        float frameTime = keyframeClock * FRAMES_PER_SEC;
        if (curFrameIndex >= clip.keyFrames.Length || curFrameIndex < 0) curFrameIndex = 0;
        AtlasKeyframe curKeyFrame = clip.keyFrames[curFrameIndex];

        if (frameTime >= curKeyFrame.holdTime)
        {
            prevFrameIndex = curFrameIndex;
            if (curFrameIndex > 0)
            {
                curFrameIndex--;
            }
            keyframeClock = 0;
        }

        return ref clip.keyFrames[curFrameIndex].motionSprite.sprite;
    }
    public static ref SimpleSprite GetNextKeyframeSpriteManual(ref AtlasClip clip, float currentTime)
    {
        int maxIndex = clip.keyFrames.Length - 1;
        int curFrameIndex = Mathf.Clamp(Mathf.FloorToInt((clip.keyFrames.Length - 1) * currentTime), 0, maxIndex);
        return ref clip.keyFrames[curFrameIndex].motionSprite.sprite;
    }
    public static ref SimpleSprite GetNextKeyframeSpriteEditor(ref AtlasClip clip, ref float keyframeClock, ref int curFrameIndex, ref int prevFrameIndex)
    {
        float frameTime = keyframeClock * FRAMES_PER_SEC;
        if (curFrameIndex >= clip.keyFrames.Length || curFrameIndex < 0) curFrameIndex = 0;
        AtlasKeyframe curKeyFrame = clip.keyFrames[curFrameIndex];

        switch (clip.clipType)
        {
            case ClipType.Loop:
            {
                if (frameTime >= curKeyFrame.holdTime)
                {
                    prevFrameIndex = curFrameIndex;
                    curFrameIndex++;

                    if (curFrameIndex >= clip.keyFrames.Length)
                    {
                        curFrameIndex = 0;
                    }

                    keyframeClock = 0;
                }
            }
            break;
            case ClipType.PingPong:
            {
                if (frameTime >= curKeyFrame.holdTime)
                {
                    if (curFrameIndex < clip.keyFrames.Length - 1 && (curFrameIndex > prevFrameIndex || curFrameIndex == 0))
                    {
                        prevFrameIndex = curFrameIndex;
                        curFrameIndex++;
                    }
                    else
                    {
                        prevFrameIndex = curFrameIndex;
                        curFrameIndex--;
                    }
                    keyframeClock = 0;
                }
            }
            break;
            case ClipType.OneShot:
            {
                if (frameTime >= curKeyFrame.holdTime)
                {
                    prevFrameIndex = curFrameIndex;
                    if (curFrameIndex < clip.keyFrames.Length - 1)
                    {
                        curFrameIndex++;
                    }
                    keyframeClock = 0;
                }
            }
            break;
        }

        return ref clip.keyFrames[curFrameIndex].motionSprite.sprite;
    }
    public static Mesh SetQuad()
    {
        Mesh quad = new Mesh();
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

public abstract class AtlasRendererBase : MonoBehaviour { }