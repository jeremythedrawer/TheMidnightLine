using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using static Atlas;
using static AtlasRendering;
using static NPC;

[ExecuteAlways]
public class AtlasRenderer : MonoBehaviour
{
    [Header("Inputs")]
    public AtlasRendererType rendererType;
    public BatchKey batchKey;
    public AtlasSO atlas;
    public CameraStatsSO camStats;
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
        if (atlas == null) return;

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
    public void FlipH(bool flipLeft, SimpleSprite sprite)
    {
        flipX = flipLeft;
        scaleAndFlip.z = flipLeft ? -1 : 1;
        float flipPivot = flipLeft ? 1 - sprite.uvPivot.x : sprite.uvPivot.x;
        worldPivotAndSize.x = flipPivot * scaleAndFlip.x * worldPivotAndSize.z;
        boundsOffset.x = (bounds.size.x * 0.5f) - worldPivotAndSize.x;
    }
    public void FlipV(bool flipDown, SimpleSprite sprite)
    {
        flipY = flipDown;
        scaleAndFlip.w = flipDown ? -1 : 1;
        float flipPivot = flipDown ? 1 - sprite.uvPivot.y : sprite.uvPivot.y;
        worldPivotAndSize.y = flipPivot * scaleAndFlip.y * worldPivotAndSize.w;
        boundsOffset.y = (bounds.size.y * 0.5f) - worldPivotAndSize.y;
    }
    public void UpdateBounds()
    {
        bounds.center = gameObject.transform.position + boundsOffset;
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
    public void PlayClip(ref AtlasClip clip)
    {
        sprite = GetNextKeyframeSprite(ref clip, ref keyframeClock, ref curFrameIndex, ref prevFrameIndex);
        UpdateSpriteInputs(ref sprite);
    }
    public void PlayClipOneShot(AtlasClip clip)
    {
        ctsOneShot?.Cancel();
        ctsOneShot = null;
        ctsOneShot = new CancellationTokenSource();

        PlayingClipOneShot(clip).Forget();
    }
    public void PlayClipReverse(AtlasClip clip)
    {
        sprite = GetNextKeyframeSpriteReverse(ref clip, ref keyframeClock, ref curFrameIndex, ref prevFrameIndex);
        UpdateSpriteInputs(ref sprite);
    }
    public void PlayClipOneShotReverse(AtlasClip clip)
    {
        ctsOneShot?.Cancel();
        ctsOneShot = null;
        ctsOneShot = new CancellationTokenSource();
        PlayingClipOneShotReverse(clip).Forget();

    }
    public void PlayManualClip(ref AtlasClip clip, float currentTime)
    {
        sprite = GetNextKeyframeSpriteManual(ref clip, currentTime);
        UpdateSpriteInputs(ref sprite);
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
        float maxLetterHeight = 0;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            int asciiIndex = (int)c;
            if (asciiIndex < 33)
            {
                if (asciiIndex == 32)
                {
                    cursorX += spacing;
                }
            }
            else
            {
                SimpleSprite sprite = atlas.simpleSprites[asciiIndex - 33];

                if (rendererType == AtlasRendererType.TextScreen)
                {
                    float letterPos = cursorX * kerning * camStats.worldUnitsPerPixel;
                    Vector4 worldPivotAndSize = worldPivotsAndSizes[spriteIndex];
                    worldPivotAndSize.x = -letterPos;

                    Vector2 spritePixelSize = sprite.worldSize * PIXELS_PER_UNIT;
                    worldPivotAndSize.z = spritePixelSize.x * camStats.worldUnitsPerPixel;
                    worldPivotAndSize.w = spritePixelSize.y * camStats.worldUnitsPerPixel;
                    worldPivotsAndSizes[spriteIndex] = worldPivotAndSize;
                    
                    cursorX += spritePixelSize.x;
                    
                    if(worldPivotAndSize.w > maxLetterHeight) maxLetterHeight = worldPivotAndSize.w;
                }
                else
                {
                    float letterPos = cursorX * kerning;
                    Vector4 worldPivotAndSize = worldPivotsAndSizes[spriteIndex];
                    worldPivotAndSize.x = -letterPos;
                    worldPivotAndSize.z = sprite.worldSize.x;
                    worldPivotAndSize.w = sprite.worldSize.y;
                    worldPivotsAndSizes[spriteIndex] = worldPivotAndSize;

                    cursorX += sprite.worldSize.x;

                    if(sprite.worldSize.y > maxLetterHeight) maxLetterHeight = sprite.worldSize.y;
                }


                uvSizesAndPositions[spriteIndex] = sprite.uvSizeAndPos;
                scalesAndFlips[spriteIndex] = Vector4.one;
                spriteIndex++;
            }
        }

        float totalTextWidth = rendererType == AtlasRendererType.TextScreen ? cursorX * kerning * camStats.worldUnitsPerPixel : cursorX * kerning;

        for(int i = 0; i < printableChars; i++)
        {
            customs[i].x = totalTextWidth;
            customs[i].y = 1.8f;
        }

        bounds.size = new Vector3(totalTextWidth,  maxLetterHeight, 0.2f);
        boundsOffset = bounds.size * 0.5f;
        
        bounds.center = new Vector3(transform.position.x + boundsOffset.x, transform.position.y + boundsOffset.y, transform.position.z);
    }
    private async UniTask PlayingClipOneShot(AtlasClip clip)
    {
        keyframeClock = 0;
        int lastIndex = clip.keyFrames.Length - 1;
        try
        {
            curFrameIndex = 0;
            while (curFrameIndex < lastIndex)
            {
                SimpleSprite nextSprite = GetNextKeyframeSprite(ref clip, ref keyframeClock, ref curFrameIndex, ref prevFrameIndex);

                if (nextSprite.index != sprite.index)
                {
                    sprite = nextSprite;
                    UpdateSpriteInputs(ref sprite);
                }
                await UniTask.Yield(ctsOneShot.Token);
            }
        }
        catch (OperationCanceledException)
        { }
    }
    private async UniTask PlayingClipOneShotReverse(AtlasClip clip)
    {
        keyframeClock = 0;
        curFrameIndex = clip.keyFrames.Length - 1;
        try
        {
            while (curFrameIndex >= 0)
            {
                SimpleSprite nextSprite = AtlasRendering.GetNextKeyframeSpriteReverse(ref clip, ref keyframeClock, ref curFrameIndex, ref prevFrameIndex);

                if (nextSprite.index != sprite.index)
                {
                    sprite = nextSprite;
                    UpdateSpriteInputs(ref sprite);
                }

                await UniTask.Yield(ctsOneShot.Token);
            }

        }
        catch (OperationCanceledException)
        { }
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