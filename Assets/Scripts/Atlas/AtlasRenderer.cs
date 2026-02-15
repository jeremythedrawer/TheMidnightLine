using UnityEditor;
using UnityEngine;
using static Atlas;
using static AtlasBatch;

[ExecuteAlways]
public class AtlasRenderer : MonoBehaviour
{
    public AtlasSO atlas;
    public Material material;
    public int spriteIndex;
    public SpriteMode spriteMode;
    public float width = 1f;
    public float height = 1f;
    public bool flipX;
    public bool flipY;
    public int depthOrder;

    [Header("Generated")]
    public BatchKey batchKey;
    public SimpleSprite sprite;
    public MotionSprite motionSprite;
    public SliceSprite slicedSprite;

    public Matrix4x4[] spriteMatrices;
    public Vector4[] widthAndHeight;
    public Vector2 flip;

    public Vector2 centerSliceWorldSize;
    public Vector2 centerSliceUVSize;
    private void OnValidate()
    {
        InitSpriteMode();
        SetCenterSliceSize();
        SetSprite(spriteIndex);
        transform.position = new Vector3(transform.position.x, transform.position.y, depthOrder);
    }

    private void Update()
    {
#if UNITY_EDITOR
        transform.position = new Vector3(transform.position.x, transform.position.y, depthOrder);
#endif

    }
    private void OnEnable()
    {
        RegisterRenderer(this);
    }
    private void OnDisable()
    {
        UnregisterRenderer(this);
    }
    public Matrix4x4 GetMatrix()
    {
        Vector2 pivotFromCenter = sprite.uvPivot;
        Vector3 pivotOffset = new Vector3(pivotFromCenter.x * sprite.worldSize.x, pivotFromCenter.y * sprite.worldSize.y, 1f);
        Vector3 matrixPos = transform.position + transform.rotation * -pivotOffset;
        Vector3 matrixScale = new Vector3(sprite.worldSize.x * width, sprite.worldSize.y * height, 1f);

        spriteMatrices[0] = Matrix4x4.TRS(matrixPos, transform.rotation, matrixScale);

        return spriteMatrices[0];
    }

    public Matrix4x4[] Get9SliceMatrices()
    {
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
    public void SetSprite(int spriteIndex)
    {
        if (atlas == null || (atlas.motionSprites.Length == 0 && atlas.slicedSprites.Length == 0 && atlas.simpleSprites.Length == 0)) return;

        switch (spriteMode)
        {
            case SpriteMode.Simple:
            {
                if (atlas.simpleSprites.Length == 0) return;
                sprite = atlas.simpleSprites[spriteIndex];

                widthAndHeight = new Vector4[]
                {
                    new Vector4(width, height, 0, 0)
                };
            }
            break;

            case SpriteMode.Motion:
            {
                if (atlas.motionSprites.Length == 0) return;
                motionSprite = atlas.motionSprites[spriteIndex];
                sprite = motionSprite.sprite;

                widthAndHeight = new Vector4[]
                {
                    new Vector4(width, height, 0, 0)
                };
            }
            break;

            case SpriteMode.Slice:
            {
                if (atlas.slicedSprites.Length == 0) return;
                slicedSprite = atlas.slicedSprites[spriteIndex];
                sprite = slicedSprite.sprite;


                widthAndHeight = new Vector4[]
                {
                    new Vector4(1, 1, 0, 0),
                    new Vector4(centerSliceUVSize.x, 1, 0, 0),
                    new Vector4(1, 1, 0, 0),
                    new Vector4(1, centerSliceUVSize.y, 0 , 0),
                    new Vector4(centerSliceUVSize.x, centerSliceUVSize.y, 0, 0),
                    new Vector4(1, centerSliceUVSize.y, 0 , 0),
                    new Vector4(1, 1, 0, 0),
                    new Vector4(centerSliceUVSize.x, 1, 0, 0),
                    new Vector4(1, 1, 0, 0),
                };
            }
            break;
        }


        flip = new Vector2(flipX ? 1 : -1, flipY ? 1 : -1);
    }
    public void Flip(bool flipLeft)
    {
        flipX = flipLeft;
    }
    public void InitSpriteMode()
    {
        if (spriteMode == SpriteMode.Slice)
        {
            spriteMatrices = new Matrix4x4[9];
        }
        else
        {
            spriteMatrices = new Matrix4x4[1];
        }
    }
    public void SetCenterSliceSize()
    {
        float totalWidthSize = sprite.worldSize.x * width;
        float totalHeightSlice = sprite.worldSize.y * height;
        centerSliceWorldSize = new Vector2(Mathf.Max(0, totalWidthSize - slicedSprite.worldSlices.x - slicedSprite.worldSlices.y), Mathf.Max(0, totalHeightSlice - slicedSprite.worldSlices.z - slicedSprite.worldSlices.w));
        centerSliceUVSize = new Vector2( Mathf.Max(0f, width - slicedSprite.slice.x - (1 - slicedSprite.slice.y)),Mathf.Max(0f, height - slicedSprite.slice.z - (1 - slicedSprite.slice.w)));
    }

#if UNITY_EDITOR
    [ExecuteAlways]
    void OnDrawGizmos()
    {
        Gizmos.matrix = GetMatrix();
        Gizmos.color = Color.clear;
        Gizmos.DrawCube(Vector3.zero, Vector3.one);
    }
#endif
}

[CustomEditor(typeof(AtlasRenderer))]
[CanEditMultipleObjects]
public class AtlasRendererEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        AtlasRenderer renderer = (AtlasRenderer)target;

        EditorGUI.BeginChangeCheck();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("atlas"));
        SerializedProperty spriteIndexProp = serializedObject.FindProperty("spriteIndex");
        SerializedProperty spriteModeProp = serializedObject.FindProperty("spriteMode");

        if (renderer.atlas != null)
        {
            SpriteMode spriteType = (SpriteMode)spriteModeProp.intValue;

            int maxIndex = 0;
            switch (spriteType)
            {
                case SpriteMode.Simple:
                {
                    if (renderer.atlas.simpleSprites.Length > 0) maxIndex = renderer.atlas.simpleSprites.Length - 1;
                }
                break;

                case SpriteMode.Motion:
                {
                    if (renderer.atlas.motionSprites.Length > 0) maxIndex = renderer.atlas.motionSprites.Length - 1;
                }
                break;

                case SpriteMode.Slice:
                {
                    if (renderer.atlas.slicedSprites.Length > 0) maxIndex = renderer.atlas.slicedSprites.Length - 1;
                }
                break;
            }

            spriteIndexProp.intValue = Mathf.Clamp(spriteIndexProp.intValue, 0, maxIndex);

            spriteIndexProp.intValue = EditorGUILayout.IntSlider("Sprite Index", spriteIndexProp.intValue, 0, maxIndex);
        }
        else
        {
            EditorGUILayout.PropertyField(spriteIndexProp);
        }

        DrawPropertiesExcluding(serializedObject, "atlas", "spriteIndex");

        
        serializedObject.ApplyModifiedProperties();
        if (EditorGUI.EndChangeCheck())
        {
            Debug.Log("Change hapeened");
            UnregisterRenderer(renderer);
            RegisterRenderer(renderer);
        }
    }
}

