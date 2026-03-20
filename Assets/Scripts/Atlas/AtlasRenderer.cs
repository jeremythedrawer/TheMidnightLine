using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.U2D;
using static Atlas;
using static AtlasBatch;

[ExecuteAlways]
public class AtlasRenderer : MonoBehaviour
{
    public AtlasSO atlas;
    public Material material;
    public BoxCollider2D boxCollider;
    public int spriteIndex;
    public SpriteMode spriteMode;
    public float width = 1f;
    public float height = 1f;
    public bool flipX;
    public bool flipY;
    public int depthOrder;
    public bool useCustomMaterial;
    [Header("Generated")]
    public MaterialPropertyBlock mpb;
    public BatchKey batchKey;
    public SimpleSprite sprite;
    public Bounds bounds;
    public Matrix4x4[] spriteMatrices;
    public Vector4[] widthHeightFlip;
    Vector2 worldPivotOffset;
    Vector2[] offsets9Slice;
    public Vector3 matrixPivotOffset;
    public float worldWidth;
    public float worldHeight;

    public float keyframeClock;
    public int curFrameIndex;
    public int prevFrameIndex;

    private void OnValidate()
    {
        InitRenderer();
    }
    private void OnEnable()
    {
        RegisterRenderer(this);
        HandleCustomMaterial();
        InitRenderer();
    }
    private void OnDisable()
    {
        UnregisterRenderer(this);
    }
    private void Start()
    {
        UpdateDepth(depthOrder);
    }
    private void Update()
    {
#if UNITY_EDITOR
        if(!Application.isPlaying)
        {
            UpdateDepth((int)transform.position.z);
        }
#endif
        bounds.center = transform.position + (Vector3)worldPivotOffset;

        if (spriteMode == SpriteMode.Slice)
        {
            Set9SliceMatrixPos();
        }
        else
        {
            SetMatrixPos();
        }
    }
    public void UpdateDepth(int newDepth)
    {
        depthOrder = newDepth;
        transform.position = new Vector3(transform.position.x, transform.position.y, depthOrder);
    }
    public void Flip(bool flipLeft)
    {
        flipX = flipLeft;
        widthHeightFlip[0].z = flipLeft ? -1 : 1;

        float flipPivot = flipX ? 1 - sprite.uvPivot.x : sprite.uvPivot.x;
        worldPivotOffset = new Vector2((0.5f - flipPivot) * worldWidth, (0.5f - flipPivot) * worldHeight);
        SetMatrix();
    }
    private void InitRenderer()
    {
        if (atlas == null) return;
        worldWidth = sprite.worldSize.x * width;
        worldHeight = sprite.worldSize.y * height;

        bounds = new Bounds();
        bounds.size = new Vector3(worldWidth, worldHeight, 0.2f);
        bounds.center = transform.position + (Vector3)worldPivotOffset;
        switch (spriteMode)
        {
            case SpriteMode.Simple:
            {
                if (atlas.simpleSprites.Length == 0) return;

                spriteIndex = Mathf.Clamp(spriteIndex, 0, atlas.simpleSprites.Length - 1);
                spriteMatrices = new Matrix4x4[1];

                sprite = atlas.simpleSprites[spriteIndex];
                widthHeightFlip = new Vector4[]
                {
                    new Vector4(width, height, flipX ? -1 : 1, flipY ? -1 : 1)
                };
                SetMatrix();

            }
            break;

            case SpriteMode.Motion:
            {
                if (atlas.motionSprites.Length == 0) return;

                spriteMatrices = new Matrix4x4[1];
                spriteIndex = Mathf.Clamp(spriteIndex, 0, atlas.motionSprites.Length - 1);
                sprite = atlas.motionSprites[spriteIndex].sprite;

                widthHeightFlip = new Vector4[]
                {
                    new Vector4(width, height, flipX ? -1 : 1, flipY ? -1 : 1)
                };
                SetMatrix();
            }
            break;

            case SpriteMode.Slice:
            {
                if (atlas.slicedSprites.Length == 0) return;

                spriteMatrices = new Matrix4x4[9];
                spriteIndex = Mathf.Clamp(spriteIndex, 0, atlas.slicedSprites.Length - 1);
                SliceSprite slicedSprite = atlas.slicedSprites[spriteIndex];

                sprite = slicedSprite.sprite;

                Vector2 centerSliceWorldSize = GetCenterSliceWorldSize(slicedSprite);
                Vector2 centerSliceUVSize = new Vector2(centerSliceWorldSize.x / (sprite.worldSize.x * (slicedSprite.slice.y - slicedSprite.slice.x)), centerSliceWorldSize.y / (sprite.worldSize.y * (slicedSprite.slice.w - slicedSprite.slice.z)));

                Vector2 flip = new Vector2(flipX ? -1 : 1, flipY ? -1 : 1);
                widthHeightFlip = new Vector4[]
                {
                    new Vector4(1, 1, flip.x, flip.y),
                    new Vector4(centerSliceUVSize.x, 1, flip.x, flip.y),
                    new Vector4(1, 1, flip.x, flip.y),
                    new Vector4(1, centerSliceUVSize.y, flip.x, flip.y),
                    new Vector4(centerSliceUVSize.x, centerSliceUVSize.y,flip.x, flip.y),
                    new Vector4(1, centerSliceUVSize.y, flip.x, flip.y),
                    new Vector4(1, 1, flip.x, flip.y),
                    new Vector4(centerSliceUVSize.x, 1,flip.x, flip.y),
                    new Vector4(1, 1, flip.x, flip.y),
                };

                Set9SliceMatrices();
            }
            break;
        }
        float flipPivotX = flipX ? 1 - sprite.uvPivot.x : sprite.uvPivot.x;
        worldPivotOffset = new Vector2((0.5f - flipPivotX) * worldWidth, (0.5f - sprite.uvPivot.y) * worldHeight);
        SetCollider();
        HandleCustomMaterial();
        transform.position = new Vector3(transform.position.x, transform.position.y, depthOrder);
    }
    private void SetCollider()
    {
        if (boxCollider == null) return;
        boxCollider.size = new Vector2(sprite.worldSize.x * width, sprite.worldSize.y * height);
        boxCollider.offset = (boxCollider.size * 0.5f) - (sprite.worldSize * new Vector2(flipX ? 1 - sprite.uvPivot.x : sprite.uvPivot.x, flipY ? 1 - sprite.uvPivot.y : sprite.uvPivot.y));
    }
    private void HandleCustomMaterial()
    {
        if (useCustomMaterial)
        {
            if (mpb == null)
            {
                mpb = new MaterialPropertyBlock();

            }
            mpb.SetVector("_UVSizeAndPos", sprite.uvSizeAndPos);
            mpb.SetVector("_WidthHeightFlip", widthHeightFlip[0]);
        }
    }
    public void PlayClip(AtlasClip clip)
    {
        keyframeClock += Time.deltaTime;

        float frameTime = keyframeClock * FRAMES_PER_SEC;
        if (curFrameIndex >= clip.keyFrames.Length || curFrameIndex < 0) curFrameIndex = 0;
        AtlasKeyframe curKeyFrame = clip.keyFrames[curFrameIndex];

        switch (clip.clipType)
        {
            case Atlas.ClipType.Loop:
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
            case Atlas.ClipType.PingPong:
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
            case Atlas.ClipType.OneShot:
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

        sprite = clip.keyFrames[curFrameIndex].motionSprite.sprite;
        SetMatrix();
    }
    public void PlayManualClip(AtlasClip clip, float currentTime)
    {
        int maxIndex = clip.keyFrames.Length - 1;
        int curFrameIndex = Mathf.Clamp(Mathf.FloorToInt((clip.keyFrames.Length - 1) * currentTime), 0, maxIndex);
        sprite = clip.keyFrames[curFrameIndex].motionSprite.sprite;
        SetMatrix();
    }
    private void Set9SliceMatrices()
    {
        SliceSprite slicedSprite = atlas.slicedSprites[spriteIndex];

        Vector2 centerSliceWorldSize = GetCenterSliceWorldSize(slicedSprite);
        
        Vector2[] sizes =
        {
            new Vector2 (slicedSprite.worldSlices.x,  slicedSprite.worldSlices.z),
            new Vector2 (centerSliceWorldSize.x, slicedSprite.worldSlices.z),
            new Vector2 (slicedSprite.worldSlices.y, slicedSprite.worldSlices.z),

            new Vector2 (slicedSprite.worldSlices.x,  centerSliceWorldSize.y),
            new Vector2 (centerSliceWorldSize.x, centerSliceWorldSize.y),
            new Vector2 (slicedSprite.worldSlices.y, centerSliceWorldSize.y),

            new Vector2 (slicedSprite.worldSlices.x,  slicedSprite.worldSlices.w),
            new Vector2 (centerSliceWorldSize.x, slicedSprite.worldSlices.w),
            new Vector2 (slicedSprite.worldSlices.y, slicedSprite.worldSlices.w),
        };

        offsets9Slice = new Vector2[]
        {
            new Vector2 (0, 0),
            new Vector2 (slicedSprite.worldSlices.x, 0),
            new Vector2 (slicedSprite.worldSlices.x + centerSliceWorldSize.x, 0),

            new Vector2 (0, slicedSprite.worldSlices.z),
            new Vector2 (slicedSprite.worldSlices.x, slicedSprite.worldSlices.z),
            new Vector2 (slicedSprite.worldSlices.x + centerSliceWorldSize.x, slicedSprite.worldSlices.z),

            new Vector2 (0, slicedSprite.worldSlices.z + centerSliceWorldSize.y),
            new Vector2 (slicedSprite.worldSlices.x, slicedSprite.worldSlices.z + centerSliceWorldSize.y),
            new Vector2 (slicedSprite.worldSlices.x + centerSliceWorldSize.x, slicedSprite.worldSlices.z + centerSliceWorldSize.y),
        };

        for (int i = 0; i < 9; i++)
        {
            Vector3 localPos = new Vector3(offsets9Slice[i].x, offsets9Slice[i].y, 0);

            spriteMatrices[i] = Matrix4x4.TRS(transform.position + transform.rotation * localPos,
                                              transform.rotation, 
                                              new Vector3(sizes[i].x, sizes[i].y, 1f));
        }
    }
    private void SetMatrix()
    {
        Vector2 pivotWithFlip = new Vector2(flipX ? 1 - sprite.uvPivot.x : sprite.uvPivot.x, flipY ? 1 - sprite.uvPivot.y : sprite.uvPivot.y);
        matrixPivotOffset = new Vector3(pivotWithFlip.x * sprite.worldSize.x, pivotWithFlip.y * sprite.worldSize.y, 0f);
        Vector3 matrixPos = transform.position + transform.rotation * -matrixPivotOffset;
        Vector3 matrixScale = new Vector3(sprite.worldSize.x * width, sprite.worldSize.y * height, 1f);

        spriteMatrices[0] = Matrix4x4.TRS(matrixPos, transform.rotation, matrixScale);
    }
    private void SetMatrixPos()
    {
        if (spriteMatrices.Length == 0) return;
        Vector3 matrixPos = transform.position + transform.rotation * -matrixPivotOffset;
        spriteMatrices[0].SetTRS(matrixPos, transform.rotation, spriteMatrices[0].lossyScale);
    }
    private void Set9SliceMatrixPos()
    {
        for (int i = 0; i < 9; i++)
        {
            Vector3 localPos = new Vector3(offsets9Slice[i].x, offsets9Slice[i].y, 0);
            Vector3 pos = transform.position + transform.rotation * localPos;
            spriteMatrices[i].SetTRS(pos, transform.rotation, spriteMatrices[i].lossyScale);
        }
    }
    private Vector2 GetCenterSliceWorldSize(SliceSprite slicedSprite)
    {
        return new Vector2(Mathf.Max(0, worldWidth - slicedSprite.worldSlices.x - slicedSprite.worldSlices.y), Mathf.Max(0, worldHeight - slicedSprite.worldSlices.z - slicedSprite.worldSlices.w));
    }
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.clear;
        Gizmos.DrawCube(bounds.center, bounds.size);
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.lawnGreen;
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }
#endif
}
