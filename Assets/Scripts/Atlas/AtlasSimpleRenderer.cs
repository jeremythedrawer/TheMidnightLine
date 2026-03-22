using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.U2D;
using static Atlas;
using static AtlasBatch;

[ExecuteAlways]
public class AtlasSimpleRenderer : MonoBehaviour
{
    public AtlasSO atlas;
    public RenderInput renderInput;

    public BoxCollider2D boxCollider;
    public int spriteIndex;
    public float width = 1f;
    public float height = 1f;
    public bool flipX;
    public bool flipY;
    public bool useCustomMaterial;
    [Header("Generated")]
    public MaterialPropertyBlock customMPB;
    public SimpleSprite sprite;
    public Bounds bounds;
    public Vector2 worldPivotOffet;

    public float keyframeClock;
    public int curFrameIndex;
    public int prevFrameIndex;

    private void OnValidate()
    {
        InitRenderer();
    }
    private void OnEnable()
    {
        InitRenderer();
        RegisterRenderer(renderInput);
    }
    private void OnDisable()
    {
        UnregisterRenderer(renderInput);
    }
    private void OnDestroy()
    {
        UnregisterRenderer(renderInput);
    }
    private void Start()
    {
        UpdateDepth(renderInput.batchKey.depthOrder);
    }
    private void Update()
    {
#if UNITY_EDITOR
        if(!Application.isPlaying)
        {
            UpdateDepth((int)transform.position.z);
        }
#endif

        bounds.center = new Vector3(transform.position.x + worldPivotOffet.x, transform.position.y + worldPivotOffet.y, transform.position.z);
        if (transform.hasChanged)
        {
            renderInput.position[0] = transform.position;
        }
    }
    public void UpdateDepth(int newDepth)
    {
        renderInput.batchKey.depthOrder = newDepth;
        transform.position = new Vector3(transform.position.x, transform.position.y, renderInput.batchKey.depthOrder);
    }
    public void Flip(bool flipLeft)
    {
        flipX = flipLeft;
        renderInput.widthHeightFlip[0].z = flipLeft ? -1 : 1;

        float flipPivot = flipX ? 1 - sprite.uvPivot.x : sprite.uvPivot.x;
        renderInput.pivot[0].x = (0.5f - flipPivot) * renderInput.widthHeightFlip[0].x;
        renderInput.pivot[0].y = (0.5f - flipPivot) * renderInput.widthHeightFlip[0].y;
    }
    private void InitRenderer()
    {
        if (atlas == null) return;
        sprite = atlas.simpleSprites[spriteIndex];

        renderInput.gameObject = gameObject;
        renderInput.uvSizeAndPos[0] = sprite.uvSizeAndPos;
        renderInput.widthHeightFlip[0].x = sprite.worldSize.x * width;
        renderInput.widthHeightFlip[0].y = sprite.worldSize.y * height;
        renderInput.widthHeightFlip[0].z = flipX ? -1 : 1;
        renderInput.widthHeightFlip[0].w = flipY ? -1 : 1;

        float flipPivot = flipX ? 1 - sprite.uvPivot.x : sprite.uvPivot.x;
        renderInput.pivot[0].x = flipPivot;
        renderInput.pivot[0].y = sprite.uvPivot.y;

        bounds = new Bounds();
        bounds.size = new Vector3(renderInput.widthHeightFlip[0].x, renderInput.widthHeightFlip[0].y, 0.2f);

        worldPivotOffet = new Vector2((sprite.worldSize.x * width) * (0.5f - sprite.uvPivot.x), (sprite.worldSize.y * height) * (0.5f - sprite.uvPivot.y));

        //case SpriteMode.Slice:
        //{
        //    if (atlas.slicedSprites.Length == 0) return;

        //    spriteMatrices = new Matrix4x4[9];
        //    spriteIndex = Mathf.Clamp(spriteIndex, 0, atlas.slicedSprites.Length - 1);
        //    SliceSprite slicedSprite = atlas.slicedSprites[spriteIndex];

        //    sprite = slicedSprite.sprite;

        //    Vector2 centerSliceWorldSize = GetCenterSliceWorldSize(slicedSprite);
        //    Vector2 centerSliceUVSize = new Vector2(centerSliceWorldSize.x / (sprite.worldSize.x * (slicedSprite.slice.y - slicedSprite.slice.x)), centerSliceWorldSize.y / (sprite.worldSize.y * (slicedSprite.slice.w - slicedSprite.slice.z)));

        //    Vector2 flip = new Vector2(flipX ? -1 : 1, flipY ? -1 : 1);
        //    widthHeightFlip = new Vector4[]
        //    {
        //        new Vector4(1, 1, flip.x, flip.y),
        //        new Vector4(centerSliceUVSize.x, 1, flip.x, flip.y),
        //        new Vector4(1, 1, flip.x, flip.y),
        //        new Vector4(1, centerSliceUVSize.y, flip.x, flip.y),
        //        new Vector4(centerSliceUVSize.x, centerSliceUVSize.y,flip.x, flip.y),
        //        new Vector4(1, centerSliceUVSize.y, flip.x, flip.y),
        //        new Vector4(1, 1, flip.x, flip.y),
        //        new Vector4(centerSliceUVSize.x, 1,flip.x, flip.y),
        //        new Vector4(1, 1, flip.x, flip.y),
        //    };
        //}


        SetCollider();
    }
    private void SetCollider()
    {
        if (boxCollider == null) return;
        boxCollider.size = new Vector2(sprite.worldSize.x * width, sprite.worldSize.y * height);
        boxCollider.offset = (boxCollider.size * 0.5f) - (sprite.worldSize * new Vector2(flipX ? 1 - sprite.uvPivot.x : sprite.uvPivot.x, flipY ? 1 - sprite.uvPivot.y : sprite.uvPivot.y));
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
    }
    public void PlayManualClip(AtlasClip clip, float currentTime)
    {
        int maxIndex = clip.keyFrames.Length - 1;
        int curFrameIndex = Mathf.Clamp(Mathf.FloorToInt((clip.keyFrames.Length - 1) * currentTime), 0, maxIndex);
        sprite = clip.keyFrames[curFrameIndex].motionSprite.sprite;
    }
    //private void Set9SliceMatrices()
    //{
    //    SliceSprite slicedSprite = atlas.slicedSprites[spriteIndex];

    //    Vector2 centerSliceWorldSize = GetCenterSliceWorldSize(slicedSprite);
        
    //    Vector2[] sizes =
    //    {
    //        new Vector2 (slicedSprite.worldSlices.x,  slicedSprite.worldSlices.z),
    //        new Vector2 (centerSliceWorldSize.x, slicedSprite.worldSlices.z),
    //        new Vector2 (slicedSprite.worldSlices.y, slicedSprite.worldSlices.z),

    //        new Vector2 (slicedSprite.worldSlices.x,  centerSliceWorldSize.y),
    //        new Vector2 (centerSliceWorldSize.x, centerSliceWorldSize.y),
    //        new Vector2 (slicedSprite.worldSlices.y, centerSliceWorldSize.y),

    //        new Vector2 (slicedSprite.worldSlices.x,  slicedSprite.worldSlices.w),
    //        new Vector2 (centerSliceWorldSize.x, slicedSprite.worldSlices.w),
    //        new Vector2 (slicedSprite.worldSlices.y, slicedSprite.worldSlices.w),
    //    };

    //    offsets9Slice = new Vector2[]
    //    {
    //        new Vector2 (0, 0),
    //        new Vector2 (slicedSprite.worldSlices.x, 0),
    //        new Vector2 (slicedSprite.worldSlices.x + centerSliceWorldSize.x, 0),

    //        new Vector2 (0, slicedSprite.worldSlices.z),
    //        new Vector2 (slicedSprite.worldSlices.x, slicedSprite.worldSlices.z),
    //        new Vector2 (slicedSprite.worldSlices.x + centerSliceWorldSize.x, slicedSprite.worldSlices.z),

    //        new Vector2 (0, slicedSprite.worldSlices.z + centerSliceWorldSize.y),
    //        new Vector2 (slicedSprite.worldSlices.x, slicedSprite.worldSlices.z + centerSliceWorldSize.y),
    //        new Vector2 (slicedSprite.worldSlices.x + centerSliceWorldSize.x, slicedSprite.worldSlices.z + centerSliceWorldSize.y),
    //    };

    //    for (int i = 0; i < 9; i++)
    //    {
    //        Vector3 localPos = new Vector3(offsets9Slice[i].x, offsets9Slice[i].y, 0);

    //        spriteMatrices[i] = Matrix4x4.TRS(transform.position + transform.rotation * localPos,
    //                                          transform.rotation, 
    //                                          new Vector3(sizes[i].x, sizes[i].y, 1f));
    //    }
    //}
    //private void SetMatrix()
    //{
    //    Vector2 pivotWithFlip = new Vector2(flipX ? 1 - sprite.uvPivot.x : sprite.uvPivot.x, flipY ? 1 - sprite.uvPivot.y : sprite.uvPivot.y);
    //    matrixPivotOffset = new Vector3(pivotWithFlip.x * sprite.worldSize.x, pivotWithFlip.y * sprite.worldSize.y, 0f);
    //    Vector3 matrixPos = transform.position + transform.rotation * -matrixPivotOffset;
    //    Vector3 matrixScale = new Vector3(sprite.worldSize.x * width, sprite.worldSize.y * height, 1f);

    //    spriteMatrices[0] = Matrix4x4.TRS(matrixPos, transform.rotation, matrixScale);
    //}
    //private void SetMatrixPos()
    //{
    //    if (spriteMatrices.Length == 0) return;
    //    Vector3 matrixPos = transform.position + transform.rotation * -matrixPivotOffset;
    //    spriteMatrices[0].m03 = matrixPos.x;
    //    spriteMatrices[0].m13 = matrixPos.y;
    //}
    //private void Set9SliceMatrixPos()
    //{
    //    for (int i = 0; i < 9; i++)
    //    {
    //        Vector3 localPos = new Vector3(offsets9Slice[i].x, offsets9Slice[i].y, 0);
    //        Vector3 pos = transform.position + transform.rotation * localPos;
    //        spriteMatrices[i].SetTRS(pos, transform.rotation, spriteMatrices[i].lossyScale);
    //    }
    //}
    //private Vector2 GetCenterSliceWorldSize(SliceSprite slicedSprite)
    //{
    //    return new Vector2(Mathf.Max(0, worldWidth - slicedSprite.worldSlices.x - slicedSprite.worldSlices.y), Mathf.Max(0, worldHeight - slicedSprite.worldSlices.z - slicedSprite.worldSlices.w));
    //}
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
