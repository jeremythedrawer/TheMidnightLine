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

    [Header("Text Settings")]
    [TextArea(3, 10)]
    public string text;
    public float kerning = 1.1f;
    public float spacing = 1;


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
    public int prevSpriteIndexFlipH;
    public int prevSpriteIndexFlipV;
    public bool isAnimating;
    public CancellationTokenSource ctsOneShot;

    [Header("Sliced Generated")]
    public SliceSprite slicedSprite;
    public int quadCount;

    private void OnValidate()
    {
        if (atlas == null) return;
        switch (rendererType)
        {
            case AtlasRendererType.SimpleScreen:
            case AtlasRendererType.SimpleWorld:
            {
                if (atlas.simpleSprites.Length == 0) { Debug.LogWarning("Atlas does not have Simple Sprites"); return; }
                spriteIndex = Mathf.Clamp(spriteIndex, 0, atlas.simpleSprites.Length - 1);
                sprite = atlas.simpleSprites[spriteIndex];
                UpdateSpriteInputs(ref sprite);
            }
            break;
            
            case AtlasRendererType.MotionScreen:
            case AtlasRendererType.MotionWorld:
            {
                if (atlas.motionSprites.Length == 0) { Debug.LogWarning("Atlas does not have Motion Sprites"); return; }
                spriteIndex = Mathf.Clamp(spriteIndex, 0, atlas.motionSprites.Length - 1);
                sprite = atlas.motionSprites[spriteIndex].sprite;
                UpdateSpriteInputs(ref sprite);
            }
            break;

            case AtlasRendererType.SliceScreen:
            case AtlasRendererType.SliceWorld:
            {
                if (atlas.slicedSprites.Length == 0) { Debug.LogWarning("Atlas does not have Sliced Sprites"); return; }
                spriteIndex = Mathf.Clamp(spriteIndex, 0, atlas.slicedSprites.Length - 1);
                slicedSprite = atlas.slicedSprites[spriteIndex];
                UpdateSlicedSpriteInputs();
            }
            break;

            case AtlasRendererType.TextScreen:
            case AtlasRendererType.TextWorld:
            {
                SetText(text);
            }
            break;
        }
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
            case AtlasRendererType.SimpleScreen:
            {
                cam = Camera.main;

            }
            break;
            case AtlasRendererType.MotionScreen:
            {
                atlas.clipDict = BuildClipKeys(atlas.clips);
                cam = Camera.main;
            }
            break;
            case AtlasRendererType.SliceScreen:
            {
                cam = Camera.main;   
            }
            break;
            case AtlasRendererType.TextScreen:
            {
                cam = Camera.main;
            }
            break;
        }
    }
    private void OnEnable()
    {
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
    public void UpdateSpriteInputs(ref SimpleSprite newSprite)
    {
        if (rendererType > AtlasRendererType.TextWorld)
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

        bounds.size = new Vector3(scaleAndFlip.x * worldPivotAndSize.z, scaleAndFlip.y * worldPivotAndSize.w, 0.2f);
        
        FlipH(flipX, newSprite);
        FlipV(flipY, newSprite);

        bounds.center = new Vector3(transform.position.x + boundsOffset.x, transform.position.y + boundsOffset.y, transform.position.z);

        if (boxCollider == null) return;
        boxCollider.size = bounds.size;
        boxCollider.offset = bounds.center - transform.position;
    }
    public void UpdateDepthEditor(Transform transform)
    {
        if ((int)transform.position.z != batchKey.depthOrder)
        {
            RegisterRenderer(this);
        }
    }
    public void UpdateDepthRealtime(int newDepth)
    {
        gameObject.transform.position = new Vector3(gameObject.transform.position.x, gameObject.transform.position.y, newDepth);
    }
    public void FlipH(bool flipLeft, SimpleSprite curSprite)
    {
        if (flipX == flipLeft && curSprite.index == prevSpriteIndexFlipH) return;

        flipX = flipLeft;
        scaleAndFlip.z = flipLeft ? -1 : 1;
        float flipPivot = flipLeft ? 1 - curSprite.uvPivot.x : curSprite.uvPivot.x;
        worldPivotAndSize.x = flipPivot * scaleAndFlip.x * worldPivotAndSize.z;
        boundsOffset.x = (bounds.size.x * 0.5f) - worldPivotAndSize.x;
        prevSpriteIndexFlipH = curSprite.index;
    }
    public void FlipV(bool flipDown, SimpleSprite curSprite)
    {
        if (flipY == flipDown && curSprite.index == prevSpriteIndexFlipV) return;

        flipY = flipDown;
        scaleAndFlip.w = flipDown ? -1 : 1;
        float flipPivot = flipDown ? 1 - curSprite.uvPivot.y : curSprite.uvPivot.y;
        worldPivotAndSize.y = flipPivot * scaleAndFlip.y * worldPivotAndSize.w;
        boundsOffset.y = (bounds.size.y * 0.5f) - worldPivotAndSize.y;
        prevSpriteIndexFlipV = curSprite.index;
    }
    public void UpdateBounds()
    {        
        bounds.center = transform.position + boundsOffset;
    }
    public void UpdateSlicedSpriteInputs()
    {
        uvSizesAndPositions = slicedSprite.uvSizeAndPos;

        float centerWorldSliceWidth = slicedSprite.sprite.worldSize.x - slicedSprite.worldSlices.x - slicedSprite.worldSlices.y;
        float centerWorldSliceHeight = slicedSprite.sprite.worldSize.y - slicedSprite.worldSlices.z - slicedSprite.worldSlices.w;

        float rightColPos = slicedSprite.worldSlices.x + (centerWorldSliceWidth * width);
        float topRowPos = slicedSprite.worldSlices.z + (centerWorldSliceHeight * height);
        worldPivotsAndSizes = new Vector4[]
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
        scalesAndFlips = new Vector4[]
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

        customs = new Vector4[9];
        bounds.size = new Vector3(slicedSprite.worldSlices.x + (centerWorldSliceWidth * width) + slicedSprite.worldSlices.y, slicedSprite.worldSlices.z + (centerWorldSliceHeight * height) + slicedSprite.worldSlices.w, 0.2f);
        boundsOffset = bounds.size * 0.5f;

        if (boxCollider == null) return;
        boxCollider.size = bounds.size;
        boxCollider.offset = boundsOffset;
    }
    public void PlayClip(ref AtlasClip clip, Transform markerTransform = null)
    {
        MotionSprite motionSprite = GetNextKeyframeSprite(ref clip, ref keyframeClock, ref curFrameIndex, ref prevFrameIndex);
        if (motionSprite.sprite.index == sprite.index) return;

        if (markerTransform != null && motionSprite.markers.Length > 0)
        {
            Vector3 markerPos = motionSprite.markers[0].objectPos;
            if (flipX) markerPos.x *= -1;
            markerPos.z = markerTransform.localPosition.z;
            markerTransform.localPosition = markerPos;
        }
        sprite = motionSprite.sprite;
        UpdateSpriteInputs(ref sprite);
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
        MotionSprite motionSprite = GetNextKeyframeSpriteReverse(ref clip, ref keyframeClock, ref curFrameIndex, ref prevFrameIndex);
        if (motionSprite.sprite.index == sprite.index) return;

        if (markerTransform != null && motionSprite.markers.Length > 0)
        {
            Vector3 markerPos = motionSprite.markers[0].objectPos;
            if (flipX) markerPos.x *= -1;
            markerPos.z = markerTransform.localPosition.z;
            markerTransform.localPosition = markerPos;
        }
        sprite = motionSprite.sprite;
        UpdateSpriteInputs(ref sprite);
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
        MotionSprite motionSprite = GetNextKeyframeSpriteManual(ref clip, currentTime);
        if (motionSprite.sprite.index == sprite.index) return;

        if (markerTransform != null && motionSprite.markers.Length > 0)
        {
            Vector3 markerPos = motionSprite.markers[0].objectPos;
            if (flipX) markerPos.x *= -1;
            markerPos.z = markerTransform.localPosition.z;
            markerTransform.localPosition = markerPos;
        }
        sprite = motionSprite.sprite;
        UpdateSpriteInputs(ref sprite);
    }
    private async UniTask PlayingClipOneShot(AtlasClip clip, Transform markerTransform = null)
    {
        keyframeClock = 0;
        int lastIndex = clip.keyFrames.Length - 1;
        try
        {
            curFrameIndex = 0;
            isAnimating = true;
            while (curFrameIndex < lastIndex)
            {
                MotionSprite motionSprite = GetNextKeyframeSprite(ref clip, ref keyframeClock, ref curFrameIndex, ref prevFrameIndex);
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
                    UpdateSpriteInputs(ref sprite);
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
        curFrameIndex = clip.keyFrames.Length - 1;
        try
        {
            isAnimating = true;
            while (curFrameIndex >= 0)
            {
                MotionSprite motionSprite = AtlasRendering.GetNextKeyframeSpriteReverse(ref clip, ref keyframeClock, ref curFrameIndex, ref prevFrameIndex);

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
                    UpdateSpriteInputs(ref sprite);
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
    public void SetText(string inputText)
    {
        text = inputText;
        float cursorX = 0;
        int printableChars = 0;

        for (int i = 0; i < inputText.Length; i++)
        {
            int asciiIndex = (int)inputText[i];
            if (asciiIndex >= 33)
            {
                printableChars++;
            }
        }

        worldPivotsAndSizes = new Vector4[printableChars];
        uvSizesAndPositions = new Vector4[printableChars];
        scalesAndFlips = new Vector4[printableChars];
        customs = new Vector4[printableChars];
        int spriteIndex = 0;
        
        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float maxY = float.MinValue;

        switch(rendererType)
        {
            case AtlasRendererType.TextScreen:
            {
                for (int i = 0; i < text.Length; i++)
                {
                    int asciiIndex = (int)text[i];
                    int letterIndex = asciiIndex - 33;
                    if (asciiIndex == 32)
                    {
                        cursorX += spacing;
                    }
                    else if (letterIndex >= 0)
                    {
                        SimpleSprite sprite = atlas.simpleSprites[letterIndex];
                        float letterPos = cursorX * kerning * camStats.worldUnitsPerPixel;
                        Vector2 spritePixelSize = sprite.worldSize * PIXELS_PER_UNIT;
                        Vector4 worldPivotAndSize = new Vector4(-letterPos, 0, spritePixelSize.x * camStats.worldUnitsPerPixel, spritePixelSize.y * camStats.worldUnitsPerPixel);
                        worldPivotsAndSizes[spriteIndex] = worldPivotAndSize;

                        float right = letterPos + worldPivotAndSize.z;

                        if (letterPos < minX) minX = letterPos;
                        if (right > maxX) maxX = right;
                        if (worldPivotAndSize.w > maxY) maxY = worldPivotAndSize.w;


                        float widthRatio = spritePixelSize.x / LETTER_ADVANCE;
                        float advanceScale = widthRatio * 0.5f + 0.5f;
                        cursorX += LETTER_ADVANCE * advanceScale;

                        uvSizesAndPositions[spriteIndex] = sprite.uvSizeAndPos;
                        scalesAndFlips[spriteIndex] = Vector4.one;
                        spriteIndex++;
                    }
                }
            }
            break;

            case AtlasRendererType.TextWorld:
            {
                for (int i = 0; i < text.Length; i++)
                {
                    int asciiIndex = (int)text[i];
                    int letterIndex = asciiIndex - 33;
                    if (asciiIndex == 32)
                    {
                        cursorX += spacing;
                    }
                    else if (letterIndex >= 0)
                    {
                        SimpleSprite sprite = atlas.simpleSprites[letterIndex];

                        float letterPos = cursorX * kerning;
                        Vector4 worldPivotAndSize = new Vector4(letterPos, 0, sprite.worldSize.x, sprite.worldSize.y);
                        worldPivotsAndSizes[spriteIndex] = worldPivotAndSize;

                        float right = worldPivotAndSize.x + worldPivotAndSize.z;

                        if (worldPivotAndSize.x < minX) minX = worldPivotAndSize.x;
                        if (right > maxX) maxX = right;
                        if (worldPivotAndSize.w > maxY) maxY = worldPivotAndSize.w;


                        float widthRatio = sprite.worldSize.x / LETTER_ADVANCE;
                        float advanceScale = widthRatio * 0.5f + 0.5f;
                        cursorX += LETTER_ADVANCE * advanceScale;

                        uvSizesAndPositions[spriteIndex] = sprite.uvSizeAndPos;
                        scalesAndFlips[spriteIndex] = Vector4.one;
                        spriteIndex++;
                    }
                }
            }
            break;
        }

        bounds.size = new Vector3(maxX, maxY, 0f);
        boundsOffset = new Vector3(maxX * 0.5f, maxY * 0.5f, 0f);
        bounds.center = transform.position + boundsOffset;
    }
    public Bounds GetTextBounds(string inputText)
    {
        float cursorX = 0;
        Bounds b = new Bounds();

        int spriteIndex = 0;

        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float maxY = float.MinValue;

        switch (rendererType)
        {
            case AtlasRendererType.TextScreen:
            {
                for (int i = 0; i < inputText.Length; i++)
                {
                    int asciiIndex = (int)inputText[i];
                    int letterIndex = asciiIndex - 33;
                    if (asciiIndex == 32)
                    {
                        cursorX += spacing;
                    }
                    else if (letterIndex >= 0)
                    {
                        SimpleSprite sprite = atlas.simpleSprites[letterIndex];
                        float letterPos = cursorX * kerning * camStats.worldUnitsPerPixel;
                        Vector2 spritePixelSize = sprite.worldSize * PIXELS_PER_UNIT;
                        Vector4 worldPivotAndSize = new Vector4(-letterPos, 0, spritePixelSize.x * camStats.worldUnitsPerPixel, spritePixelSize.y * camStats.worldUnitsPerPixel);

                        float right = letterPos + worldPivotAndSize.z;

                        if (letterPos < minX) minX = letterPos;
                        if (right > maxX) maxX = right;
                        if (worldPivotAndSize.w > maxY) maxY = worldPivotAndSize.w;

                        float widthRatio = spritePixelSize.x / LETTER_ADVANCE;
                        float advanceScale = widthRatio * 0.5f + 0.5f;
                        cursorX += LETTER_ADVANCE * advanceScale;

                        spriteIndex++;
                    }
                }
            }
            break;

            case AtlasRendererType.TextWorld:
            {
                for (int i = 0; i < inputText.Length; i++)
                {
                    int asciiIndex = (int)inputText[i];
                    int letterIndex = asciiIndex - 33;
                    if (asciiIndex == 32)
                    {
                        cursorX += spacing;
                    }
                    else if (letterIndex >= 0)
                    {
                        SimpleSprite sprite = atlas.simpleSprites[letterIndex];

                        float letterPos = cursorX * kerning;
                        Vector4 worldPivotAndSize = new Vector4(letterPos, 0, sprite.worldSize.x, sprite.worldSize.y);

                        float right = worldPivotAndSize.x + worldPivotAndSize.z;

                        if (worldPivotAndSize.x < minX) minX = worldPivotAndSize.x;
                        if (right > maxX) maxX = right;
                        if (worldPivotAndSize.w > maxY) maxY = worldPivotAndSize.w;


                        float widthRatio = sprite.worldSize.x / LETTER_ADVANCE;
                        float advanceScale = widthRatio * 0.5f + 0.5f;
                        cursorX += LETTER_ADVANCE * advanceScale;

                        spriteIndex++;
                    }
                }
            }
            break;
        }

        b.size = new Vector3(maxX, maxY, 0f);
        Vector3 bOffset = new Vector3(maxX * 0.5f, maxY * 0.5f, 0f);
        b.center = transform.position + bOffset;

        return b;
    }
    public void AppearPreviewText()
    {
        Debug.Log("Appearing text");
        AppearingText().Forget();
    }
    public void AppearConfirmText()
    {
        for (int i = 0; i < customs.Length; i++)
        {
            customs[i].x = 0;
        }
    }
    private async UniTask AppearingText()
    {
        float elapsedTime = 0;
        while (elapsedTime < APPEAR_TEXT_TIME)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / APPEAR_TEXT_TIME;
            t *= 0.5f;
            t = 1 - t;
            for (int i = 0; i < customs.Length; i++)
            {
                customs[i].x = t;
            }
            await UniTask.Yield();
        }
    }
    public void SetScrollingText()
    {
        customs = new Vector4[worldPivotsAndSizes.Length];
        for (int i = 0; i < customs.Length; i++)
        {
            customs[i].x = bounds.size.x;
            customs[i].y = 1.8f;
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

                // Key
                EditorGUILayout.LabelField(kvp.Key.ToString(), GUILayout.MaxWidth(150));

                // Value (AtlasClip reference)
                EditorGUILayout.LabelField(kvp.Value.clipName, GUILayout.MaxWidth(150));

                EditorGUILayout.EndHorizontal();
            }

            EditorGUI.indentLevel--;
        }
    }
}
#endif