using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using static Atlas;
using static AtlasBatch;

[ExecuteAlways]
public class AtlasRenderer : MonoBehaviour
{
    public AtlasSO atlas;
    public int spriteIndex;
    public SpriteType spriteType;

    [Header("Generated")]
    public BatchKey batchKey;
    
    public SimpleSprite sprite;
    public Vector4 uvSizeAndPos;
    public Matrix4x4 spriteMatrix;
    float worldWidth;
    float worldHeight;
    public bool flip;

    private void OnValidate()
    {
        SetSprite(spriteIndex);
        RegisterRenderer(this);
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
        Vector2 pivotFromCenter = sprite.uvPivot - new Vector2(0.5f, 0.5f);
        Vector3 pivotOffset = new Vector3(-pivotFromCenter.x * worldWidth, -pivotFromCenter.y * worldHeight, 0f);
        Vector3 matrixPos = transform.position + transform.rotation * pivotOffset;
        Vector3 matrixScale = new Vector3(worldWidth, worldHeight, 1f);
        spriteMatrix = Matrix4x4.TRS(matrixPos, transform.rotation, matrixScale);
        return spriteMatrix;
    }

    public void SetSprite(int spriteIndex)
    {
        if (atlas == null || (atlas.motionSprites.Length == 0 && atlas.slicedSprites.Length == 0 && atlas.simpleSprites.Length == 0)) return;

        switch(spriteType)
        {
            case SpriteType.Simple:
            {
                if (atlas.simpleSprites.Length == 0) return;
                sprite = atlas.simpleSprites[spriteIndex];
            }
            break;

            case SpriteType.Motion:
            {
                if (atlas.motionSprites.Length == 0) return;
                sprite = atlas.motionSprites[spriteIndex].sprite;
            }
            break;

            case SpriteType.Slice:
            {
                if (atlas.slicedSprites.Length == 0) return;

                sprite = atlas.slicedSprites[spriteIndex].sprite;
            }
            break;
        }

        float spritePixelWidth = sprite.uvSize.x * atlas.texture.width;
        float spritePixelHeight = sprite.uvSize.y * atlas.texture.height;

        worldWidth = spritePixelWidth / PIXELS_PER_UNIT;
        worldHeight = spritePixelHeight / PIXELS_PER_UNIT;

        Vector2 offset = sprite.uvPos + (sprite.uvSize * 0.5f);
        
        int flipSprite = flip ? -1 : 1;
        uvSizeAndPos = new Vector4(sprite.uvSize.x * flipSprite, sprite.uvSize.y, offset.x, offset.y);
    }

    public void Flip(bool flipLeft)
    {
        flip = flipLeft;
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

            spriteIndexProp.intValue = EditorGUILayout.IntSlider("Sprite Index", spriteIndexProp.intValue, 0, maxIndex );
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

