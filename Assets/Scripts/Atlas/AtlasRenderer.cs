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
    public SpriteType spriteType;
    public float width = 1f;
    public float height = 1f;
    public bool flipX;
    public bool flipY;

    [Header("Generated")]
    public BatchKey batchKey;
    public SimpleSprite sprite;
    public MotionSprite motionSprite;
    public SliceSprite sliceSprite;

    public Matrix4x4[] spriteMatrices;
    public Vector4[] widthAndHeight;
    public Vector2 flip;
    private void OnValidate()
    {
        if (spriteType == SpriteType.Slice)
        {
            spriteMatrices = new Matrix4x4[9];
        }
        else
        {
            spriteMatrices = new Matrix4x4[1];
            widthAndHeight = new Vector4[1];
        }
        SetSprite(spriteIndex);
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
        Vector3 pivotOffset = new Vector3(pivotFromCenter.x * sprite.worldSize.x, pivotFromCenter.y * sprite.worldSize.y, 0f);
        Vector3 matrixPos = transform.position + transform.rotation * -pivotOffset;
        Vector3 matrixScale = new Vector3(sprite.worldSize.x * width, sprite.worldSize.y * height, 1f);

        spriteMatrices[0] = Matrix4x4.TRS(matrixPos, transform.rotation, matrixScale);

        return spriteMatrices[0];
    }

    public Matrix4x4[] Get9SliceMatrices()
    {
        //TODO: Cache these variables
        float worldLeftSlice = (sliceSprite.slice.x * atlas.texture.width) / PIXELS_PER_UNIT;
        float worldRightSlice = ((1 - sliceSprite.slice.y) * atlas.texture.width) / PIXELS_PER_UNIT;
        float worldBottomSlice = (sliceSprite.slice.z * atlas.texture.height) / PIXELS_PER_UNIT;
        float worldTopSlice = ((1 - sliceSprite.slice.w) * atlas.texture.height) / PIXELS_PER_UNIT;

        float widthSlice = sprite.worldSize.x * width;
        float heightSlice = sprite.worldSize.y * height;

        float centerWidth = Mathf.Max(0, widthSlice - worldLeftSlice - worldRightSlice);
        float centerHeight = Mathf.Max(0, heightSlice - worldBottomSlice - worldTopSlice);

        Vector2[] sizes =
        {
            new Vector2 (worldLeftSlice,  worldBottomSlice),
            new Vector2 (centerWidth, worldBottomSlice),
            new Vector2 (worldRightSlice, worldBottomSlice),

            new Vector2 (worldLeftSlice,  centerHeight),
            new Vector2 (centerWidth, centerHeight),
            new Vector2 (worldRightSlice, centerHeight),

            new Vector2 (worldLeftSlice,  worldTopSlice),
            new Vector2 (centerWidth, worldTopSlice),
            new Vector2 (worldRightSlice, worldTopSlice),
        };


        Vector2[] offsets =
        {
            new Vector2 (0, 0),
            new Vector2 (worldLeftSlice, 0),
            new Vector2 (worldLeftSlice + centerWidth, 0),

            new Vector2 (0, worldBottomSlice),
            new Vector2 (worldLeftSlice, worldBottomSlice),
            new Vector2 (worldLeftSlice + centerWidth, worldBottomSlice),

            new Vector2 (0, worldBottomSlice + centerHeight),
            new Vector2 (worldLeftSlice, worldBottomSlice + centerHeight),
            new Vector2 (worldLeftSlice + centerWidth, worldBottomSlice + centerHeight),
        };

        for (int i = 0; i < 9; i++)
        {
            Vector3 localPos = new Vector3(offsets[i].x, offsets[i].y, 0f);

            spriteMatrices[i] = Matrix4x4.TRS(transform.position + transform.rotation * localPos, transform.rotation, new Vector3(sizes[i].x, sizes[i].y, 1f));
        }

        return spriteMatrices;
    }

    public void SetSprite(int spriteIndex)
    {
        if (atlas == null || (atlas.motionSprites.Length == 0 && atlas.slicedSprites.Length == 0 && atlas.simpleSprites.Length == 0)) return;

        switch (spriteType)
        {
            case SpriteType.Simple:
            {
                if (atlas.simpleSprites.Length == 0) return;
                sprite = atlas.simpleSprites[spriteIndex];

                widthAndHeight = new Vector4[]
                {
                    new Vector4(width, height, 0, 0)
                };
            }
            break;

            case SpriteType.Motion:
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

            case SpriteType.Slice:
            {
                if (atlas.slicedSprites.Length == 0) return;
                sliceSprite = atlas.slicedSprites[spriteIndex];
                sprite = sliceSprite.sprite;

                widthAndHeight = new Vector4[]
                {
                    new Vector4(1, 1, 0, 0),
                    new Vector4(width, 1, 0, 0),
                    new Vector4(1, 1, 0, 0),
                    new Vector4(1, height, 0 , 0),
                    new Vector4(width, height, 0, 0),
                    new Vector4(1, height, 0 , 0),
                    new Vector4(1, 1, 0, 0),
                    new Vector4(width, 1, 0, 0),
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
        SerializedProperty spriteTypeProp = serializedObject.FindProperty("spriteType");

        if (renderer.atlas != null)
        {
            SpriteType spriteType = (SpriteType)spriteTypeProp.intValue;

            int maxIndex = 0;
            switch (spriteType)
            {
                case SpriteType.Simple:
                {
                    if (renderer.atlas.simpleSprites.Length > 0) maxIndex = renderer.atlas.simpleSprites.Length - 1;
                }
                break;

                case SpriteType.Motion:
                {
                    if (renderer.atlas.motionSprites.Length > 0) maxIndex = renderer.atlas.motionSprites.Length - 1;
                }
                break;

                case SpriteType.Slice:
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

        if (EditorGUI.EndChangeCheck())
        {

            renderer.SetSprite(spriteIndexProp.intValue);
        }

        serializedObject.ApplyModifiedProperties();
    }
}

