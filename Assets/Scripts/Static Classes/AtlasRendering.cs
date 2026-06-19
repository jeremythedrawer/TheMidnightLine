using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using static Atlas;
public static class AtlasRendering
{
    public const int MAX_SPRITE_DATA_COUNT = 1024;
    public static int SPRITE_DATA_STRIDE = Marshal.SizeOf<SpriteData>();

    public static int ARGS_STRIDE = INT_SIZE * 5;

    public enum AtlasRendererType
    { 
        SimpleWorld,
        MotionWorld,
        SliceWorld,
    }
    public enum AtlasTextRendererType
    {
        Simple,
        Scroll,
        Border,
    }
    public enum AtlasTextAlignmentType
    {
        Left,
        Center,
        Right,
    }


    [Serializable] public struct TextBoxData
    {
        public Vector2 size;
        public float[] lineWidths;
    }


    [Serializable] public struct BatchKey
    {
        public Texture2D texture;
        public Material material;
    }
    [Serializable] public struct SpriteData
    {
        public Vector4 worldPosition;
        public Vector4 worldPivotAndSize;
        public Vector4 uvSizeAndPos;
        public Vector4 scaleAndFlip;
        public Vector4 custom;
    }

    public static Dictionary<BatchKey, SpriteBatch> spriteBatchDict = new Dictionary<BatchKey, SpriteBatch>();
    public static List<(BatchKey, SpriteBatch)> spriteBatchList = new List<(BatchKey, SpriteBatch)> ();

    public static Dictionary<BatchKey, TextBatch> textBatchDict = new Dictionary<BatchKey, TextBatch>();
    public static List<(BatchKey, TextBatch)> textBatchList = new List<(BatchKey, TextBatch)>();

    public class SpriteBatch
    {
        public List<AtlasRenderer> atlasRendererList = new List<AtlasRenderer>();

        public SpriteData[] spriteData = new SpriteData[MAX_SPRITE_DATA_COUNT];

        public GraphicsBuffer spriteDataBuffer;
        public GraphicsBuffer argsBuffer;

        public MaterialPropertyBlock mpb;
    }
    public class TextBatch
    {
        public List<AtlasTextRenderer> atlasTextRendererList = new List<AtlasTextRenderer>();

        public SpriteData[] spriteData = new SpriteData[MAX_SPRITE_DATA_COUNT];

        public GraphicsBuffer spriteDataBuffer;
        public GraphicsBuffer argsBuffer;

        public MaterialPropertyBlock mpb;
    }

    public static void RefreshFrame()
    {
        spriteBatchList.Clear();
        foreach (KeyValuePair<BatchKey, SpriteBatch> kv in spriteBatchDict)
        {
            BatchKey key = kv.Key;
            SpriteBatch batch = kv.Value;
            spriteBatchList.Add((key, batch));
        }
        textBatchList.Clear();

        foreach (KeyValuePair<BatchKey, TextBatch> kv in textBatchDict)
        {
            BatchKey key = kv.Key;
            TextBatch batch = kv.Value;
            textBatchList.Add((key, batch));
        }
    }
    public static void RegisterRenderer(AtlasRenderer renderer)
    {
        if (renderer.batchKey.material == null) return;
        renderer.batchKey.material.enableInstancing = true;

        UnregisterRenderer(renderer);

        if (!spriteBatchDict.TryGetValue(renderer.batchKey, out SpriteBatch batch))
        {
            batch = new SpriteBatch();
            batch.spriteDataBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, MAX_SPRITE_DATA_COUNT, SPRITE_DATA_STRIDE);
            batch.argsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, ARGS_STRIDE);
            batch.mpb = new MaterialPropertyBlock();

            spriteBatchDict.Add(renderer.batchKey, batch);
        }

        batch.atlasRendererList.Add(renderer);
    }
    public static void UnregisterRenderer(AtlasRenderer renderer)
    {
        if (!spriteBatchDict.TryGetValue(renderer.batchKey, out SpriteBatch batch)) return;

        batch.atlasRendererList.Remove(renderer);
        if (batch.atlasRendererList.Count == 0)
        {
            batch.spriteDataBuffer?.Release();
            batch.spriteDataBuffer = null;

            batch.argsBuffer?.Release();
            batch.argsBuffer = null;

            spriteBatchDict.Remove(renderer.batchKey);

        }
    }
    public static void RegisterTextRenderer(AtlasTextRenderer renderer)
    {
        if (renderer.batchKey.material == null) return;
        renderer.batchKey.material.enableInstancing = true;

        UnregisterTextRenderer(renderer);
      //  renderer.batchKey.depthOrder = (int)renderer.transform.position.z;

        if (!textBatchDict.TryGetValue(renderer.batchKey, out TextBatch batch))
        {
            batch = new TextBatch();
            
            batch.spriteDataBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, MAX_SPRITE_DATA_COUNT, SPRITE_DATA_STRIDE);
            batch.argsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, ARGS_STRIDE);
            batch.mpb = new MaterialPropertyBlock();

            textBatchDict.Add(renderer.batchKey, batch);
        }
        batch.atlasTextRendererList.Add(renderer);
    }
    public static void UnregisterTextRenderer(AtlasTextRenderer renderer)
    {
        if (!textBatchDict.TryGetValue(renderer.batchKey, out TextBatch batch)) return;

        batch.atlasTextRendererList.Remove(renderer);
        if (batch.atlasTextRendererList.Count == 0)
        {
            batch.spriteDataBuffer?.Release();
            batch.spriteDataBuffer = null;

            batch.argsBuffer?.Release();
            batch.argsBuffer = null;

            textBatchDict.Remove(renderer.batchKey);
        }
    }
    public static MotionSprite GetNextKeyframeIndex(AtlasSO atlas, AtlasClip clip, ref float keyframeClock, ref int curFrameIndex, ref int prevFrameIndex)
    {
        if (curFrameIndex > clip.keyframeEndIndex || curFrameIndex < clip.keyframeStartIndex) curFrameIndex = clip.keyframeStartIndex;

        keyframeClock += Time.deltaTime;

        MotionSprite curMotionSprite = atlas.motionSprites[curFrameIndex];
        int curFrame = (int)(keyframeClock * FRAMES_PER_SEC);
     
        if (curFrame < curMotionSprite.holdFrames) return curMotionSprite;

        switch (clip.clipType)
        {
            case ClipType.Loop:
            {
                prevFrameIndex = curFrameIndex;
                curFrameIndex++;

                if (curFrameIndex > clip.keyframeEndIndex)
                {
                    curFrameIndex = clip.keyframeStartIndex;
                }

                keyframeClock = 0;
            }
            break;
            case ClipType.PingPong:
            {
                if (curFrameIndex < clip.keyframeEndIndex && (curFrameIndex > prevFrameIndex || curFrameIndex == clip.keyframeStartIndex))
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
            break;
            case ClipType.OneShot:
            {
                prevFrameIndex = curFrameIndex;
                if (curFrameIndex < clip.keyframeEndIndex)
                {
                    curFrameIndex++;
                }
                keyframeClock = 0;
            }
            break;
        }

        return atlas.motionSprites[curFrameIndex];
    }
    public static MotionSprite GetNextKeyframeSpriteReverse(AtlasSO atlas, AtlasClip clip, ref float keyframeClock, ref int curFrameIndex, ref int prevFrameIndex)
    {
        if (curFrameIndex > clip.keyframeEndIndex || curFrameIndex < clip.keyframeStartIndex) curFrameIndex = clip.keyframeEndIndex;

        keyframeClock += Time.deltaTime;

        MotionSprite curMotionSprite = atlas.motionSprites[curFrameIndex];
        int curFrame = (int)(keyframeClock * FRAMES_PER_SEC);
        if (curFrame < curMotionSprite.holdFrames) return curMotionSprite;

        prevFrameIndex = curFrameIndex;
        if (curFrameIndex > clip.keyframeStartIndex) curFrameIndex--;
        keyframeClock = 0;

        return atlas.motionSprites[curFrameIndex];
    }
    public static MotionSprite GetNextKeyframeSpriteManual(AtlasSO atlas, AtlasClip clip, float currentTime)
    {
        int curFrameIndex = (int)Mathf.Lerp(clip.keyframeStartIndex, clip.keyframeEndIndex, currentTime);
        return atlas.motionSprites[curFrameIndex];
    }
    public static MotionSprite GetNextKeyframeIndexEditor(AtlasSO atlas, AtlasClip clip, ref float keyframeClock, ref int curFrameIndex, ref int prevFrameIndex)
    {
        if (curFrameIndex > clip.keyframeEndIndex || curFrameIndex < 0) curFrameIndex = 0;

        MotionSprite curMotionSprite = atlas.motionSprites[curFrameIndex];
        int curFrame = (int)(keyframeClock * FRAMES_PER_SEC);

        if (curFrame < curMotionSprite.holdFrames) return curMotionSprite;

        switch (clip.clipType)
        {
            case ClipType.Loop:
            {
                prevFrameIndex = curFrameIndex;
                curFrameIndex++;

                if (curFrameIndex > clip.keyframeEndIndex)
                {
                    curFrameIndex = clip.keyframeStartIndex;
                }

                keyframeClock = 0;
            }
            break;
            case ClipType.PingPong:
            {
                if (curFrameIndex < clip.keyframeEndIndex && (curFrameIndex > prevFrameIndex || curFrameIndex == clip.keyframeStartIndex))
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
            break;
            case ClipType.OneShot:
            {
                prevFrameIndex = curFrameIndex;
                if (curFrameIndex < clip.keyframeEndIndex)
                {
                    curFrameIndex++;
                }
                keyframeClock = 0;
            }
            break;
        }

        return atlas.motionSprites[curFrameIndex];
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
    public static Vector4[] GetScaleAndFlipSliceNineSliceArray(float width, float height)
    {
        Vector4 scaleFlipCQuad = Vector4.one;
        Vector4 scaleFlipHQuad = new Vector4(width, 1, 1, 1);
        Vector4 scaleFlipVQuad = new Vector4(1, height, 1, 1);
        Vector4 scaleFlipMQuad = new Vector4(width, height, 1, 1);

        return new Vector4[]
        {
            scaleFlipCQuad,
            scaleFlipHQuad,
            scaleFlipCQuad,

            scaleFlipVQuad,
            scaleFlipMQuad,
            scaleFlipVQuad,

            scaleFlipCQuad,
            scaleFlipHQuad,
            scaleFlipCQuad
        };
    }
    public static Vector4[] GetQuadScalesNineSlice(float width, float height, SliceSprite sliceSprite)
    {

        float centerWidth = sliceSprite.sprite.worldSize.x - sliceSprite.worldSlices.x - sliceSprite.worldSlices.y;
        float centerHeight = sliceSprite.sprite.worldSize.y - sliceSprite.worldSlices.z - sliceSprite.worldSlices.w;

        float pivotWidth = (width - 1) * centerWidth;
        float pivotHeight = (height - 1) * centerHeight;
        return new Vector4[]
        {
            new Vector4(1, 1, 0, 0),
            new Vector4(width, 1, 0, 0),
            new Vector4(1, 1, pivotWidth, 0),

            new Vector4(1, height, 0, 0),
            new Vector4(width, height, 0, 0),
            new Vector4(1, height, pivotWidth, 0),

            new Vector4(1, 1, 0, pivotHeight),
            new Vector4(width, 1, 0, pivotHeight),
            new Vector4(1, 1, pivotWidth, pivotHeight),
        };
    }
    public static Vector4[] SetWorldPivotAndSizes(SliceSprite sliceSprite, float width, float height)
    {
        float centerWorldSliceWidth = sliceSprite.sprite.worldSize.x - sliceSprite.worldSlices.x - sliceSprite.worldSlices.y;
        float centerWorldSliceHeight = sliceSprite.sprite.worldSize.y - sliceSprite.worldSlices.z - sliceSprite.worldSlices.w;

        float rightColPos = sliceSprite.worldSlices.x + (centerWorldSliceWidth * width);
        float topRowPos = sliceSprite.worldSlices.z + (centerWorldSliceHeight * height);

        float basePivotX = sliceSprite.sprite.uvPivot.x * -(rightColPos + sliceSprite.worldSlices.y);
        float basePivotY = sliceSprite.sprite.uvPivot.y * -(topRowPos + sliceSprite.worldSlices.y);


        return new Vector4[]
        {
            new Vector4(basePivotX, basePivotY, sliceSprite.worldSlices.x, sliceSprite.worldSlices.z),
            new Vector4(basePivotX + sliceSprite.worldSlices.x, basePivotY, centerWorldSliceWidth, sliceSprite.worldSlices.z),
            new Vector4(basePivotX + rightColPos, basePivotY, sliceSprite.worldSlices.y, sliceSprite.worldSlices.z),

            new Vector4(basePivotX, basePivotY + sliceSprite.worldSlices.z, sliceSprite.worldSlices.x, centerWorldSliceHeight),
            new Vector4(basePivotX + sliceSprite.worldSlices.x, basePivotY + sliceSprite.worldSlices.z, centerWorldSliceWidth, centerWorldSliceHeight),
            new Vector4(basePivotX + rightColPos, basePivotY + sliceSprite.worldSlices.z, sliceSprite.worldSlices.y, centerWorldSliceHeight),

            new Vector4(basePivotX, basePivotY+ topRowPos, sliceSprite.worldSlices.x, sliceSprite.worldSlices.w),
            new Vector4(basePivotX + sliceSprite.worldSlices.x,basePivotY + topRowPos, centerWorldSliceWidth, sliceSprite.worldSlices.w),
            new Vector4(basePivotX + rightColPos, basePivotY + topRowPos, sliceSprite.worldSlices.y, sliceSprite.worldSlices.w),
        };
    }
}