using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEditor;
using UnityEngine;
using static Atlas;
using static AtlasRendering;

[ExecuteAlways]
public class AtlasRenderer : MonoBehaviour
{
    [Header("Required Inputs")]
    public AtlasRendererType rendererType;
    public BatchKey batchKey;
    public AtlasSO atlas;

    [Header("Screen Inputs")]
    public CameraStatsSO camStats;

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
                sprite = atlas.simpleSprites[spriteIndex];
                UpdateSpriteInputs(sprite);
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

        FlipH(flipX);
        FlipV(flipY);
        
        batchKey.texture = atlas.texture;
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
        UnregisterRenderer(this);
    }
    private void OnDestroy()
    {
        UnregisterRenderer(this);
    }
    private void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UpdateDepthEditor(transform);
        }
#endif
    }
    public void UpdateSpriteInputs(SimpleSprite newSprite)
    {
        sprite = newSprite;
        spriteIndex = newSprite.index;
        if (rendererType > AtlasRendererType.SliceWorld)
        {
            Vector2 spritePixelSize = newSprite.worldSize * PIXELS_PER_UNIT;
            worldPivotAndSize.z = spritePixelSize.x * camStats.worldUnitsPerPixel;
            worldPivotAndSize.w = spritePixelSize.y * camStats.worldUnitsPerPixel;
        }
        else
        {
            worldPivotAndSize.z = newSprite.worldSize.x;
            worldPivotAndSize.w = newSprite.worldSize.y;
        }

        uvSizeAndPosition = newSprite.uvSizeAndPos;
        
        scaleAndFlip.x = width;
        scaleAndFlip.y = height;

        
        FlipH(flipX);
        FlipV(flipY);
        SetBounds();


        if (boxCollider == null) return;
        boxCollider.size = bounds.size;
        boxCollider.offset = bounds.center - transform.position;
        spriteIndex = sprite.index;
    }
    public void UpdateDepthEditor(Transform transform)
    {
        //if ((int)transform.position.z != batchKey.depthOrder)
        //{
        //    RegisterRenderer(this);
        //}
    }
    public void SetBounds()
    {
        bounds.size = new Vector3(scaleAndFlip.x * worldPivotAndSize.z, scaleAndFlip.y * worldPivotAndSize.w, 0.2f);
        bounds.center = new Vector3(transform.position.x + boundsOffset.x, transform.position.y + boundsOffset.y, transform.position.z);
    }
    public Bounds GetBounds()
    {
        SetBounds();
        return bounds;
    }
    public void UpdateDepthRealtime(int newDepth)
    {
        transform.position = new Vector3(transform.position.x, transform.position.y, newDepth);
    }
    public void FlipH(bool flipLeft)
    {
        flipX = flipLeft;
        scaleAndFlip.z = flipLeft ? -1 : 1;
        float flipPivot = flipLeft ? 1 - sprite.uvPivot.x : sprite.uvPivot.x;
        worldPivotAndSize.x = -flipPivot * scaleAndFlip.x * worldPivotAndSize.z;
        boundsOffset.x = (bounds.size.x * 0.5f) + worldPivotAndSize.x;
        prevSpriteIndexFlipH = sprite.index;
    }
    public void FlipV(bool flipDown)
    {
        flipY = flipDown;
        scaleAndFlip.w = flipDown ? -1 : 1;
        float flipPivot = flipDown ? 1 - sprite.uvPivot.y : sprite.uvPivot.y;
        worldPivotAndSize.y = -flipPivot * scaleAndFlip.y * worldPivotAndSize.w;
        boundsOffset.y = (bounds.size.y * 0.5f) + worldPivotAndSize.y;
        prevSpriteIndexFlipV = sprite.index;
    }
    public void UpdateBounds()
    {        
        bounds.center = transform.position + boundsOffset;
    }
    public void UpdateSlicedSpriteInputs(SliceSprite sliceSprite)
    {
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
        boundsOffset = bounds.size * 0.5f;

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
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.indigo;
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }
#endif
}

#if UNITY_EDITOR

[CustomEditor(typeof(AtlasRenderer))]
public class AtlasRendererEditor : Editor
{
    private bool showClipDict = true;

    public override void OnInspectorGUI()
    {
        // Draw normal inspector
        DrawDefaultInspector();

        AtlasRenderer renderer = (AtlasRenderer)target;

        if (renderer.atlas == null)
            return;

        if (renderer.atlas.clipDict == null)
        {
            EditorGUILayout.HelpBox("clipDict is null. Make sure it's built.", MessageType.Warning);
            return;
        }

        EditorGUILayout.Space();
        showClipDict = EditorGUILayout.Foldout(showClipDict, "Clip Dictionary", true);

        if (showClipDict)
        {
            EditorGUI.indentLevel++;

            foreach (KeyValuePair<int, AtlasClip> kvp in renderer.atlas.clipDict)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(kvp.Key.ToString(), GUILayout.MaxWidth(150));
                EditorGUILayout.LabelField(kvp.Value.clipName, GUILayout.MaxWidth(150));

                EditorGUILayout.EndHorizontal();
            }

            EditorGUI.indentLevel--;
        }
    }
}
#endif