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

    public static readonly Dictionary<BatchKey, BatchData> batchDict = new Dictionary<BatchKey, BatchData>();
    public static readonly List<(BatchKey key, BatchData)> batchList = new List<(BatchKey key, BatchData data)>();

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
    [Serializable] public class SingularRenderInput
    {
        public BatchKey batchKey;
        public AtlasSO atlas;
        public BoxCollider2D boxCollider;

        public bool flipX;
        public bool flipY;
        
        [Header("Generated")]
        public GameObject gameObject;

        public Bounds bounds = new Bounds();

        public Vector4 pivotAndSize;
        public Vector4 uvSizeAndPos;
        public Vector4 scaleAndFlip;
        public Vector4 custom;

        public Vector3 boundsOffset;

        public void UpdateRenderInputsScreen(float width, float height, SimpleSprite newSprite, CameraStatsSO camStats)
        {
            if (atlas == null) return;
            Vector2 spritePixelSize = newSprite.worldSize * PIXELS_PER_UNIT;
            uvSizeAndPos = newSprite.uvSizeAndPos;
            pivotAndSize.z = spritePixelSize.x * camStats.worldUnitsPerPixel;
            pivotAndSize.w = spritePixelSize.y * camStats.worldUnitsPerPixel;
            scaleAndFlip.x = width;
            scaleAndFlip.y = height;

            bounds.size = new Vector3(scaleAndFlip.x * pivotAndSize.z, scaleAndFlip.y * pivotAndSize.w, 0.2f);
            FlipH(flipX, newSprite);
            FlipV(flipY, newSprite);

            bounds.center = new Vector3(gameObject.transform.position.x + boundsOffset.x, gameObject.transform.position.y + boundsOffset.y, gameObject.transform.position.z);
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
        public void UpdateDepthEditor(Transform transform)
        {
            AtlasRendering.UpdateDepth(transform, ref batchKey, singRenderInput: this);
        }
        public void UpdateDepthRealtime(int newDepth)
        {
            gameObject.transform.position = new Vector3(gameObject.transform.position.x, gameObject.transform.position.y, newDepth);
           // AtlasRendering.UpdateDepth(gameObject.transform, ref batchKey, singRenderInput: this);
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
            bounds.center = gameObject.transform.position + boundsOffset;
        }
    }
    [Serializable] public class MultipleRenderInput
    {
        public BatchKey batchKey;
        public AtlasSO atlas;
        public BoxCollider2D boxCollider;

        [Header("Generated")]
        public GameObject gameObject;
        public Bounds bounds = new Bounds();

        public Vector4[] worldPivotAndSize;
        public Vector4[] uvSizeAndPos;
        public Vector4[] scaleAndFlip;
        
        public Vector3 boundsOffset;


        public void UpdateSlicedSprite(SliceSprite slicedSprite, float width, float height)
        {
            uvSizeAndPos = slicedSprite.uvSizeAndPos;

            float centerWorldSliceWidth = slicedSprite.sprite.worldSize.x - slicedSprite.worldSlices.x - slicedSprite.worldSlices.y;
            float centerWorldSliceHeight = slicedSprite.sprite.worldSize.y - slicedSprite.worldSlices.z - slicedSprite.worldSlices.w;

            float rightColPos = slicedSprite.worldSlices.x + (centerWorldSliceWidth * width);
            float topRowPos = slicedSprite.worldSlices.z + (centerWorldSliceHeight * height);
            worldPivotAndSize = new Vector4[]
            {
                new Vector4(0, 0, slicedSprite.worldSlices.x, slicedSprite.worldSlices.z),
                new Vector4(-slicedSprite.worldSlices.x, 0, centerWorldSliceWidth, slicedSprite.worldSlices.z),
                new Vector4(-rightColPos, 0, slicedSprite.worldSlices.y, slicedSprite.worldSlices.z),

                new Vector4(0, -slicedSprite.worldSlices.z, slicedSprite.worldSlices.x, centerWorldSliceHeight),
                new Vector4(-slicedSprite.worldSlices.x, -slicedSprite.worldSlices.z, centerWorldSliceWidth, centerWorldSliceHeight),
                new Vector4(-rightColPos, -slicedSprite.worldSlices.z, slicedSprite.worldSlices.y, centerWorldSliceHeight),

                new Vector4(0, -topRowPos, slicedSprite.worldSlices.x, slicedSprite.worldSlices.w),
                new Vector4(-slicedSprite.worldSlices.x, -topRowPos, centerWorldSliceWidth, slicedSprite.worldSlices.w),
                new Vector4(-rightColPos, -topRowPos, slicedSprite.worldSlices.y, slicedSprite.worldSlices.w),
            };

            Vector4 scaleFlipCQuad = Vector4.one;
            Vector4 scaleFlipHQuad = new Vector4(width, 1, 1, 1);
            Vector4 scaleFlipVQuad = new Vector4(1, height, 1, 1);
            Vector4 scaleFlipMQuad = new Vector4(width, height, 1, 1);
            scaleAndFlip = new Vector4[]
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
            bounds.size = new Vector3(slicedSprite.worldSlices.x + (centerWorldSliceWidth * width) + slicedSprite.worldSlices.y, slicedSprite.worldSlices.z + (centerWorldSliceHeight * height) + slicedSprite.worldSlices.w, 0.2f);
            boundsOffset = bounds.size * 0.5f;

            if (boxCollider == null) return;
            boxCollider.size = bounds.size;
            boxCollider.offset = boundsOffset;
        }
        public void UpdateDepthEditor(Transform transform)
        {
            AtlasRendering.UpdateDepth(transform, ref batchKey, multRenderInput: this);
        }
        public void UpdateDepthRealtime(int newDepth)
        {
            gameObject.transform.position = new Vector3(gameObject.transform.position.x, gameObject.transform.position.y, newDepth);
        }
        public void InitRenderer(GameObject obj)
        {
            gameObject = obj;
        }
        public void UpdateBounds()
        {
            bounds.center = gameObject.transform.position + boundsOffset;
        }
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
    public static void UpdateDepth(Transform transform, ref BatchKey batchKey, SingularRenderInput singRenderInput = null, MultipleRenderInput multRenderInput = null)
    {
        if ((int)transform.position.z != batchKey.depthOrder)
        {
            if (singRenderInput != null)
            {
                RegisterSingleRenderInput(singRenderInput);
            }
            else
            {
                RegisterMultipleRenderInput(multRenderInput);
            }
        }
    }
    public static void PrepareFrame()
    {
        batchList.Clear();

        foreach (var kv in batchDict)
        {
            BatchKey key = kv.Key;
            BatchData batch = kv.Value;
            batchList.Add((key, batch));
        }
    }
    public static void RegisterSingleRenderInput(SingularRenderInput renderInput)
    {
        if (renderInput.batchKey.material == null) return;
        renderInput.batchKey.material.enableInstancing = true;

        UnregisterSingleRenderInput(renderInput);

        renderInput.batchKey.depthOrder = (int)renderInput.gameObject.transform.position.z;
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

        renderInput.batchKey.depthOrder = (int)renderInput.gameObject.transform.position.z;

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

public abstract class AtlasRenderer : MonoBehaviour { }