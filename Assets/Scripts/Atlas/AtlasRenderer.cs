using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.IMGUI.Controls;
#endif

using static Atlas;
using static AtlasRendering;

[ExecuteAlways]
public class AtlasRenderer : MonoBehaviour
{
    [Header("Required Inputs")]
    public AtlasRendererType rendererType;
    public BatchKey batchKey;
    public AtlasSO atlas;

    [Header("Collisions")]
    public BoxCollider2D boxCollider;

    [Header("Settings")]
    public int spriteIndex;

    public bool flipX;
    public bool flipY;

    public float width = 1;
    public float height = 1;

    [Header("Generated")]
    public Camera cam;
    public SimpleSprite sprite;
    public Bounds bounds = new Bounds();
    public Vector4 worldPivotAndSize;
    public Vector4 uvSizeAndPosition;
    public Vector4 scaleAndFlip;
    public Vector4 custom;
    public int customBit;


    public Vector3 boundsOffset;

    public Vector4[] worldPivotsAndSizes;
    public Vector4[] uvSizesAndPositions;
    public Vector4[] scalesAndFlips;
    public Vector4[] customs;

    [Header("Motion Generated")]
    public float keyframeClock;
    public int curFrameIndex;
    public int prevFrameIndex;
    public int curMotionIndex;
    public int prevSpriteIndexFlipH;
    public int prevSpriteIndexFlipV;
    public bool isAnimating;
    public CancellationTokenSource ctsOneShot;

    [Header("Sliced Generated")]
    public int quadCount;

    private void OnValidate()
    {
        if (atlas == null) return;

        switch (rendererType)
        {
            case AtlasRendererType.SimpleWorld:
            {
                if (atlas.simpleSprites.Length == 0) { Debug.LogWarning("Atlas does not have Simple Sprites at: " + name); return; }
                spriteIndex = Mathf.Clamp(spriteIndex, 0, atlas.simpleSprites.Length - 1);
                UpdateSpriteInputsByIndex(spriteIndex);
            }
            break;
            
            case AtlasRendererType.MotionWorld:
            {
                if (atlas.motionSprites.Length == 0) { Debug.LogWarning("Atlas does not have Motion Sprites at: " + name); return; }
                spriteIndex = Mathf.Clamp(spriteIndex, 0, atlas.motionSprites.Length - 1);
                sprite = atlas.motionSprites[spriteIndex].sprite;
                UpdateSpriteInputs(sprite);
                atlas.clipDict = BuildClipKeys(atlas.clips);
            }
            break;

            case AtlasRendererType.SliceWorld:
            {
                if (atlas.slicedSprites.Length == 0) { Debug.LogWarning("Atlas does not have Sliced Sprites at: " + name); return; }
                spriteIndex = Mathf.Clamp(spriteIndex, 0, atlas.slicedSprites.Length - 1);
                UpdateSlicedSpriteInputs(atlas.slicedSprites[spriteIndex]);
            }
            break;
        }
        
        batchKey.texture = atlas.texture;
        RegisterRenderer(this);

    }
    private void Awake()
    {
        switch (rendererType)
        {
            case AtlasRendererType.MotionWorld:
            {
                atlas.clipDict = BuildClipKeys(atlas.clips);
            }
            break;
        }
    }
    private void OnEnable()
    {
        if (batchKey.texture == null) return;
        batchKey.texture = atlas.texture;
        RegisterRenderer(this);
    }
    private void OnDisable()
    {
        ctsOneShot?.Cancel();
        UnregisterRenderer(this);
    }
    private void OnDestroy()
    {
        UnregisterRenderer(this);
    }
    public void UpdateSpriteInputsByIndex(int index)
    {
        UpdateSpriteInputs(atlas.simpleSprites[index]);
    }
    public void UpdateSpriteInputs(SimpleSprite newSprite)
    {
        sprite = newSprite;
        spriteIndex = newSprite.index;
        worldPivotAndSize.z = newSprite.worldSize.x;
        worldPivotAndSize.w = newSprite.worldSize.y;
        uvSizeAndPosition = newSprite.uvSizeAndPos;
        
        scaleAndFlip.x = width;
        scaleAndFlip.y = height;

        
        FlipHSimple(flipX);
        FlipVSimple(flipY);
        SetBounds();


        if (boxCollider == null) return;
        boxCollider.size = bounds.size;
        boxCollider.offset = bounds.center - transform.position;
        spriteIndex = sprite.index;
    }
    public void SetBounds()
    {

        switch(rendererType)
        {
            case AtlasRendererType.SliceWorld:
            {
                SliceSprite sliceSprite = atlas.slicedSprites[spriteIndex];
                Vector4 centerWorldPivot = worldPivotsAndSizes[4];
                bounds.center = new Vector3(transform.position.x + boundsOffset.x, transform.position.y + boundsOffset.y, transform.position.z);
                bounds.size = new Vector3(sliceSprite.worldSlices.x + (centerWorldPivot.z * width) + sliceSprite.worldSlices.y, sliceSprite.worldSlices.z + (centerWorldPivot.w * height) + sliceSprite.worldSlices.w, 0.2f);
            }
            break;
            case AtlasRendererType.MotionWorld:
            case AtlasRendererType.SimpleWorld:
            {
                bounds.center = new Vector3(transform.position.x + boundsOffset.x, transform.position.y + boundsOffset.y, transform.position.z);
                bounds.size = new Vector3(scaleAndFlip.x * worldPivotAndSize.z, scaleAndFlip.y * worldPivotAndSize.w, 0.2f);
            }
            break;
        }
    }
    public Bounds GetBounds()
    {
        SetBounds();
        return bounds;
    }
    public void UpdateWorldDepth(int newDepth)
    {
        transform.position = new Vector3(transform.position.x, transform.position.y, newDepth);
    }
    public void FlipHSimple(bool flipLeft)
    {
        flipX = flipLeft;
        scaleAndFlip.z = flipLeft ? -1 : 1;
        float flipPivot = flipLeft ? 1 - sprite.uvPivot.x : sprite.uvPivot.x;
        worldPivotAndSize.x = -flipPivot * scaleAndFlip.x * worldPivotAndSize.z;
        boundsOffset.x = (scaleAndFlip.x * worldPivotAndSize.z * 0.5f) + worldPivotAndSize.x;
        prevSpriteIndexFlipH = sprite.index;
    }
    public void FlipVSimple(bool flipDown)
    {
        flipY = flipDown;
        scaleAndFlip.w = flipDown ? -1 : 1;
        float flipPivot = flipDown ? 1 - sprite.uvPivot.y : sprite.uvPivot.y;
        worldPivotAndSize.y = -flipPivot * scaleAndFlip.y * worldPivotAndSize.w;
        boundsOffset.y = (scaleAndFlip.y * worldPivotAndSize.w * 0.5f) + worldPivotAndSize.y;
        prevSpriteIndexFlipV = sprite.index;
    }

    public void FlipHSlice(bool flipLeft, SliceSprite sliceSprite)
    {
        flipX = flipLeft;
        float flipPivot = flipLeft ? 1 - sprite.uvPivot.x : sprite.uvPivot.x;

        worldPivotAndSize.x = -flipPivot * (width * worldPivotsAndSizes[4].z) - (sprite.uvPivot.x * (sliceSprite.worldSlices.x + sliceSprite.worldSlices.y));

        boundsOffset.x = (bounds.size.x * 0.5f) + worldPivotAndSize.x;
        prevSpriteIndexFlipH = sprite.index;
    }
    public void FlipVSlice(bool flipDown, SliceSprite sliceSprite)
    {
        flipY = flipDown;

        float flipPivot = flipDown ? 1 - sprite.uvPivot.y : sprite.uvPivot.y;

        worldPivotAndSize.x = -flipPivot * (width * worldPivotsAndSizes[4].w) - (sprite.uvPivot.y * (sliceSprite.worldSlices.z + sliceSprite.worldSlices.w));

        boundsOffset.y = (bounds.size.y * 0.5f) + worldPivotAndSize.y;
        prevSpriteIndexFlipH = sprite.index;

    }
    public void UpdateBounds()
    {        
        bounds.center = transform.position + boundsOffset;
    }
    public void UpdateSliceSpriteInputsSelf()
    {
        UpdateSlicedSpriteInputs(atlas.slicedSprites[spriteIndex]);
    }
    public void UpdateSlicedSpriteInputs(SliceSprite sliceSprite)
    {
        sprite = sliceSprite.sprite;

        scaleAndFlip.x = width;
        scaleAndFlip.y = height;

        uvSizesAndPositions = new Vector4[9];
        Array.Copy(sliceSprite.uvSizeAndPos, uvSizesAndPositions, sliceSprite.uvSizeAndPos.Length);
        
        worldPivotsAndSizes = SetWorldPivotAndSizes(sliceSprite, width, height);

        scalesAndFlips = GetScaleAndFlipSliceNineSliceArray(width, height);

        customs = new Vector4[9];
        for (int i = 0;  i < customs.Length; i++)
        {
            customs[i] = custom;
        }
        Vector4 centerWorldPivot = worldPivotsAndSizes[4];
        bounds.size = new Vector3(sliceSprite.worldSlices.x + (centerWorldPivot.z * width) + sliceSprite.worldSlices.y, sliceSprite.worldSlices.z + (centerWorldPivot.w * height) + sliceSprite.worldSlices.w, 0.2f);

        FlipHSlice(flipX, sliceSprite);
        FlipVSlice(flipY, sliceSprite);

        if (boxCollider == null) return;
        boxCollider.size = bounds.size;
        boxCollider.offset = boundsOffset;


    }
    public void SetWidthFromWorldSpace(float worldWidth, SimpleSprite sprite)
    {
        width = worldWidth / sprite.worldSize.x;
        UpdateSpriteInputs(sprite);
    }
    public void SetNineSliceSizeFromWorldSpace(Vector2 worldSize, SliceSprite sliceSprite)
    {
        float centerWorldSliceWidth = sliceSprite.sprite.worldSize.x - sliceSprite.worldSlices.x - sliceSprite.worldSlices.y;
        float centerWorldSliceHeight = sliceSprite.sprite.worldSize.y - sliceSprite.worldSlices.z - sliceSprite.worldSlices.w;
        width = worldSize.x / centerWorldSliceWidth;
        height = worldSize.y / centerWorldSliceHeight;
        UpdateSlicedSpriteInputs(sliceSprite);
    }
    public void PlayClip(ref AtlasClip clip, Transform markerTransform = null)
    {
        if (clip.motionIndex != curMotionIndex)
        {
            curFrameIndex = clip.keyframeStartIndex;
            curMotionIndex = clip.motionIndex;
        }
        MotionSprite motionSprite = GetNextKeyframeIndex(atlas, clip, ref keyframeClock, ref curFrameIndex, ref prevFrameIndex);

        if (motionSprite.sprite.index == sprite.index) return;

        if (markerTransform != null && motionSprite.markers.Length > 0)
        {
            Vector3 markerPos = motionSprite.markers[0].objectPos;
            if (flipX) markerPos.x *= -1;
            markerPos.z = markerTransform.localPosition.z;
            markerTransform.localPosition = markerPos;
        }
        sprite = motionSprite.sprite;
        UpdateSpriteInputs(sprite);
        isAnimating = true;
        
    }
    public void PlayClipOneShot(AtlasClip clip, Transform markerTransform = null)
    {
        ctsOneShot?.Cancel();
        ctsOneShot = null;
        ctsOneShot = new CancellationTokenSource();

        PlayingClipOneShot(clip, markerTransform).Forget();
    }
    public void PlayClipReverse(AtlasClip clip, Transform markerTransform = null)
    {
        MotionSprite motionSprite = GetNextKeyframeSpriteReverse(atlas, clip, ref keyframeClock, ref curFrameIndex, ref prevFrameIndex);
        if (motionSprite.sprite.index == sprite.index) return;

        if (markerTransform != null && motionSprite.markers.Length > 0)
        {
            Vector3 markerPos = motionSprite.markers[0].objectPos;
            if (flipX) markerPos.x *= -1;
            markerPos.z = markerTransform.localPosition.z;
            markerTransform.localPosition = markerPos;
        }
        sprite = motionSprite.sprite;
        UpdateSpriteInputs(sprite);
    }
    public void PlayClipOneShotReverse(AtlasClip clip, Transform markerTransform = null)
    {
        ctsOneShot?.Cancel();
        ctsOneShot = null;
        ctsOneShot = new CancellationTokenSource();
        PlayingClipOneShotReverse(clip, markerTransform).Forget();

    }
    public void PlayManualClip(ref AtlasClip clip, float currentTime, Transform markerTransform = null)
    {
        MotionSprite motionSprite = GetNextKeyframeSpriteManual(atlas, clip, currentTime);
        if (motionSprite.sprite.index == sprite.index) return;

        if (markerTransform != null && motionSprite.markers.Length > 0)
        {
            Vector3 markerPos = motionSprite.markers[0].objectPos;
            if (flipX) markerPos.x *= -1;
            markerPos.z = markerTransform.localPosition.z;
            markerTransform.localPosition = markerPos;
        }
        sprite = motionSprite.sprite;
        UpdateSpriteInputs(sprite);
    }
    private async UniTask PlayingClipOneShot(AtlasClip clip, Transform markerTransform = null)
    {
        keyframeClock = 0;
        int lastIndex = clip.keyframeEndIndex;
        try
        {
            curFrameIndex = clip.keyframeStartIndex;
            isAnimating = true;
            while (curFrameIndex < lastIndex)
            {
                MotionSprite motionSprite = GetNextKeyframeIndex(atlas, clip, ref keyframeClock, ref curFrameIndex, ref prevFrameIndex);
                if (motionSprite.sprite.index != sprite.index)
                {
                    if (markerTransform != null && motionSprite.markers.Length > 0)
                    {
                        Vector3 markerPos = motionSprite.markers[0].objectPos;
                        if (flipX) markerPos.x *= -1;
                        markerPos.z = markerTransform.localPosition.z;
                        markerTransform.localPosition = markerPos;
                    }
                    sprite = motionSprite.sprite;
                    UpdateSpriteInputs(sprite);
                }
                await UniTask.Yield(ctsOneShot.Token);
            }
            isAnimating = false;
        }
        catch (OperationCanceledException)
        { 
            isAnimating = false;
        }
    }
    private async UniTask PlayingClipOneShotReverse(AtlasClip clip, Transform markerTransform = null)
    {
        keyframeClock = 0;
        curFrameIndex = clip.keyframeEndIndex;
        try
        {
            isAnimating = true;
            while (curFrameIndex > clip.keyframeStartIndex)
            {
                MotionSprite motionSprite = GetNextKeyframeSpriteReverse(atlas, clip, ref keyframeClock, ref curFrameIndex, ref prevFrameIndex);

                if (motionSprite.sprite.index != sprite.index)
                {
                    if (markerTransform != null && motionSprite.markers.Length > 0)
                    {
                        Vector3 markerPos = motionSprite.markers[0].objectPos;
                        if (flipX) markerPos.x *= -1;
                        markerPos.z = markerTransform.localPosition.z;
                        markerTransform.localPosition = markerPos;
                    }
                    sprite = motionSprite.sprite;
                    UpdateSpriteInputs(sprite);
                }

                await UniTask.Yield(ctsOneShot.Token);
            }
            isAnimating = false;

        }
        catch (OperationCanceledException)
        {
            isAnimating = false;
        }
    }
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.clear;
        Gizmos.DrawCube(bounds.center, bounds.size);
    }
#endif
}

#if UNITY_EDITOR
[CustomEditor(typeof(AtlasRenderer))]
public class AtlasRendererEditor : Editor
{
    BoxBoundsHandle boundsHandle = new BoxBoundsHandle();
    private void OnSceneGUI()
    {
        AtlasRenderer rend = (AtlasRenderer)target;

        if (Selection.activeGameObject == rend.gameObject)
        {
            EditorGUI.BeginChangeCheck();

            boundsHandle.center = rend.transform.position + rend.boundsOffset;

            switch (rend.rendererType)
            {
                case AtlasRendererType.SliceWorld:
                {
                    SliceSprite sliceSprite = rend.atlas.slicedSprites[rend.spriteIndex];

                    Vector4 centerWorldPivot = rend.worldPivotsAndSizes[4];
                    boundsHandle.size = new Vector3(sliceSprite.worldSlices.x + (centerWorldPivot.z * rend.width) + sliceSprite.worldSlices.y, sliceSprite.worldSlices.z + (centerWorldPivot.w * rend.height) + sliceSprite.worldSlices.w, 0.2f);
                }
                break;
                case AtlasRendererType.MotionWorld:
                case AtlasRendererType.SimpleWorld:
                {
                    boundsHandle.size = new Vector3(rend.sprite.worldSize.x * rend.width, rend.sprite.worldSize.y * rend.height, 0.2f);
                }
                break;
            }
        }

        boundsHandle.SetColor(Color.green);
        boundsHandle.DrawHandle();

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(rend, "Resize Bounds");

            switch (rend.rendererType)
            {
                case AtlasRendererType.SliceWorld:
                {
                    SliceSprite sliceSprite = rend.atlas.slicedSprites[rend.spriteIndex];
                    Vector4 centerWorldPivot = rend.worldPivotsAndSizes[4];
                    float fixWidth = sliceSprite.worldSlices.x + sliceSprite.worldSlices.y;
                    float fixHeight = sliceSprite.worldSlices.z + sliceSprite.worldSlices.w;

                    rend.width = (boundsHandle.size.x - fixWidth) / centerWorldPivot.z;
                    rend.height = (boundsHandle.size.y - fixHeight) / centerWorldPivot.w;
                    rend.UpdateSlicedSpriteInputs(sliceSprite);
                }
                break;
                case AtlasRendererType.MotionWorld:
                case AtlasRendererType.SimpleWorld:
                {
                    rend.width = boundsHandle.size.x / rend.sprite.worldSize.x;
                    rend.height = boundsHandle.size.y / rend.sprite.worldSize.y;
                    rend.UpdateSpriteInputs(rend.sprite);
                }
                break;
            }

        }

    }
}
#endif