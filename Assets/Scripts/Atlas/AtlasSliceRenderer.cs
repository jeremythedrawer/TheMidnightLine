using UnityEngine;
using static Atlas;
using static AtlasBatch;

[ExecuteAlways]
public class AtlasSliceRenderer : MonoBehaviour
{
    public AtlasSO atlas;
    public MultipleRenderInput renderInput;
    public BoxCollider2D boxCollider;
    public int spriteIndex;
    public float width;
    public float height;
    [Header("Generated")]
    public SliceSprite slicedSprite;
    public Vector2 boundOffset;
    public int quadCount;

    private void Awake()
    {
        InitRenderer();
    }

    private void OnValidate()
    {
        if (atlas == null) return;
        spriteIndex = Mathf.Clamp(spriteIndex, 0, atlas.slicedSprites.Length - 1);

        UpdateSprite();
        SetCollider();
        renderInput.bounds.center = new Vector3(transform.position.x + boundOffset.x, transform.position.y + boundOffset.y, transform.position.z);
    }
    private void OnEnable()
    {
        InitRenderer();
        SetCollider();
        RegisterMultipleRenderInput(renderInput);
    }
    private void OnDisable()
    {
        UnregisterMultipleRenderInput(renderInput);
    }

    private void OnDestroy()
    {
        UnregisterMultipleRenderInput(renderInput);
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UpdateDepth((int)transform.position.z);
        }
#endif

        if (transform.hasChanged)
        {
            renderInput.bounds.center = new Vector3(transform.position.x + boundOffset.x, transform.position.y + boundOffset.y, transform.position.z);
        }
    }
    public void UpdateSprite()
    {
        slicedSprite = atlas.slicedSprites[spriteIndex];

        renderInput.uvSizeAndPos = slicedSprite.uvSizeAndPos;

        float centerWorldSliceWidth = slicedSprite.sprite.worldSize.x - slicedSprite.worldSlices.x - slicedSprite.worldSlices.y;
        float centerWorldSliceHeight = slicedSprite.sprite.worldSize.y - slicedSprite.worldSlices.z - slicedSprite.worldSlices.w;

        float rightColPos = slicedSprite.worldSlices.x + (centerWorldSliceWidth * width);
        float topRowPos = slicedSprite.worldSlices.z + (centerWorldSliceHeight * height);
        renderInput.worldPivotAndSize = new Vector4[]
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
        renderInput.scaleAndFlip = new Vector4[]
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
        renderInput.bounds.size = new Vector3(slicedSprite.worldSlices.x + (centerWorldSliceWidth * width) + slicedSprite.worldSlices.y, slicedSprite.worldSlices.z + (centerWorldSliceHeight * height) + slicedSprite.worldSlices.w, 0.2f);
        boundOffset = renderInput.bounds.size * 0.5f;
        SetCollider();
    }
    public void UpdateDepth(int newDepth)
    {
        renderInput.batchKey.depthOrder = newDepth;
        transform.position = new Vector3(transform.position.x, transform.position.y, renderInput.batchKey.depthOrder);
    }
    private void InitRenderer()
    {
        if (atlas == null) return;
        renderInput.gameObject = gameObject;
    }
    private void SetCollider()
    {
        if (boxCollider == null) return;
        boxCollider.size = renderInput.bounds.size;
        boxCollider.offset = renderInput.bounds.center - transform.position;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.clear;
        Gizmos.DrawCube(renderInput.bounds.center, renderInput.bounds.size);
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.indigo;
        Gizmos.DrawWireCube(renderInput.bounds.center, renderInput.bounds.size);
    }
#endif
}
