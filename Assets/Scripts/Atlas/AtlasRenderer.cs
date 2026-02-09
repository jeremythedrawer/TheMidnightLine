using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using static Atlas;
using static AtlasBatch;

[ExecuteAlways]
public class AtlasRenderer : MonoBehaviour
{
    public AtlasBaseSO atlas;
    public int spriteIndex;

    [Header("Generated")]
    public BatchKey batchKey;
    public AtlasSprite sprite;
    public Vector4 uvSizeAndPos;

    public Vector3 pivotOffset;
    public float worldWidth;
    public float worldHeight;
    public int flip;
    private void OnEnable()
    {
        RegisterRenderer(this);
    }

    private void OnDisable()
    {
        UnregisterRenderer(this);
    }
    private void OnValidate()
    {
        SetSpriteUV();
    }
    public Matrix4x4 GetMatrix()
    {
        
        return Matrix4x4.TRS((transform.position + transform.rotation * pivotOffset), transform.rotation, new Vector3(worldWidth, worldHeight, 1f));
    }

    public void SetSpriteUV()
    {
        sprite = atlas.sprites[spriteIndex];

        float spritePixelWidth = sprite.uvSize.x * atlas.texture.width;
        float spritePixelHeight = sprite.uvSize.y * atlas.texture.height;

        worldWidth = spritePixelWidth / PIXELS_PER_UNIT;
        worldHeight = spritePixelHeight / PIXELS_PER_UNIT;

        Vector2 pivotFromCenter = sprite.uvPivot - new Vector2(0.5f, 0.5f);

        pivotOffset = new Vector3(
            -pivotFromCenter.x * worldWidth,
            -pivotFromCenter.y * worldHeight,
            0f
        );

        Vector2 offset = sprite.uvPos + (sprite.uvSize * 0.5f);
        uvSizeAndPos = new Vector4(sprite.uvSize.x * flip, sprite.uvSize.y, offset.x, offset.y);
    }

    public void Flip(bool flipLeft)
    {
    }

#if UNITY_EDITOR
    [ExecuteAlways]
    void OnDrawGizmos()
    {
        Gizmos.matrix = GetMatrix();
        Gizmos.color = new Color(1, 0, 0, 0.1f);
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

        if (renderer.atlas != null && renderer.atlas.sprites != null)
        {
            spriteIndexProp.intValue = EditorGUILayout.IntSlider("Sprite Index", spriteIndexProp.intValue, leftValue: 0, renderer.atlas.sprites.Length - 1);
        }
        else
        {
            EditorGUILayout.PropertyField(spriteIndexProp);
        }
        DrawPropertiesExcluding(serializedObject, "atlas", "spriteIndex");

        if (EditorGUI.EndChangeCheck())
        {
            renderer.SetSpriteUV();
        }

        serializedObject.ApplyModifiedProperties();
    }
}

