using System.Linq;
using UnityEditor;
using UnityEngine;
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
    public BatchKey batchKey;
    public SimpleSprite sprite;

    public Matrix4x4[] spriteMatrices;
    public Vector4[] widthHeightFlip;

    public float worldWidth;
    public float worldHeight;
    public MaterialPropertyBlock mpb;
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
    private void Update()
    {
        transform.position = new Vector3(transform.position.x, transform.position.y, depthOrder);
    }
    public void Flip(bool flipLeft)
    {
        flipX = flipLeft;
        widthHeightFlip[0].z = flipLeft ? -1 : 1;
    }
    private void InitRenderer()
    {
        worldWidth = sprite.worldSize.x * width;
        worldHeight = sprite.worldSize.y * height;
        switch (spriteMode)
        {
            case SpriteMode.Simple:
            {
                if (atlas.simpleSprites.Length == 0) return;

                spriteMatrices = new Matrix4x4[1];

                sprite = atlas.simpleSprites[spriteIndex];

                widthHeightFlip = new Vector4[]
                {
                    new Vector4(width, height, flipX ? -1 : 1, flipY ? -1 : 1)
                };
            }
            break;

            case SpriteMode.Motion:
            {
                if (atlas.motionSprites.Length == 0) return;

                spriteMatrices = new Matrix4x4[1];

                sprite = atlas.motionSprites[spriteIndex].sprite;

                widthHeightFlip = new Vector4[]
                {
                    new Vector4(width, height, flipX ? -1 : 1, flipY ? -1 : 1)
                };
            }
            break;

            case SpriteMode.Slice:
            {
                if (atlas.slicedSprites.Length == 0) return;

                spriteMatrices = new Matrix4x4[9];

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
            }
            break;
        }

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
    public Matrix4x4[] Get9SliceMatrices()
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

        Vector2[] offsets =
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
            Vector3 localPos = new Vector3(offsets[i].x, offsets[i].y, 0);

            spriteMatrices[i] = Matrix4x4.TRS(transform.position + transform.rotation * localPos,
                                              transform.rotation, 
                                              new Vector3(sizes[i].x, sizes[i].y, 1f));
        }

        return spriteMatrices;
    }
    public Matrix4x4 GetMatrix()
    {
        Vector2 pivotWithFlip = new Vector2(flipX ? 1 - sprite.uvPivot.x : sprite.uvPivot.x, flipY ? 1 - sprite.uvPivot.y : sprite.uvPivot.y);
        Vector3 pivotOffset = new Vector3(pivotWithFlip.x * sprite.worldSize.x, pivotWithFlip.y * sprite.worldSize.y, 0f);
        Vector3 matrixPos = transform.position + transform.rotation * -pivotOffset;
        Vector3 matrixScale = new Vector3(sprite.worldSize.x * width, sprite.worldSize.y * height, 1f);

        spriteMatrices[0] = Matrix4x4.TRS(matrixPos, transform.rotation, matrixScale);

        return spriteMatrices[0];
    }
    private Vector2 GetCenterSliceWorldSize(SliceSprite slicedSprite)
    {
        return new Vector2(Mathf.Max(0, worldWidth - slicedSprite.worldSlices.x - slicedSprite.worldSlices.y), Mathf.Max(0, worldHeight - slicedSprite.worldSlices.z - slicedSprite.worldSlices.w));
    }
#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.matrix = GetMatrix();
        Gizmos.color = Color.clear;
        Gizmos.DrawCube(Vector3.zero, Vector3.one);
    }
#endif
}
