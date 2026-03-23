using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using static Atlas;
using static AtlasBatch;
[ExecuteAlways]
public class AtlasUISimpleRenderer : MonoBehaviour
{
    public SingularRenderInput renderInput;
    public CameraStatsSO camStats;

    [Header("UI Simple (User)")]
    public float width;
    public float height;
    public int spriteIndex;
    [Header("Simple (Generated)")]
    public SimpleSprite sprite;

    [Header("Generated")]
    public Vector2 boundOffset;
    public Vector3 screenPosition;

#if UNITY_EDITOR
    [Header("Editor")]
    public Vector3 lastPos;
#endif

    private void Awake()
    {
        renderInput.InitRenderer(gameObject);
    }

    private void OnValidate()
    {
        spriteIndex = Mathf.Clamp(spriteIndex, 0, renderInput.atlas.simpleSprites.Length - 1);
        sprite = renderInput.atlas.simpleSprites[spriteIndex];
        renderInput.UpdateRenderInputsScreen(width, height, sprite, camStats, transform);
    }
    private void OnEnable()
    {
        renderInput.InitRenderer(gameObject);
        RegisterSingleRenderInput(renderInput);

#if UNITY_EDITOR
        Selection.selectionChanged += UpdateEditorPosition;
#endif
    }

    private void OnDisable()
    {
        UnregisterSingleRenderInput(renderInput);
    }

    private void OnDestroy()
    {
        UnregisterSingleRenderInput(renderInput);
    }
    private void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            renderInput.UpdateDepth((int)transform.position.z);
            //SetSprite();
        }
#endif
    }

#if UNITY_EDITOR
    public void UpdateEditorPosition()
    {
        if (Selection.activeGameObject == this.gameObject)
        {
            Vector3 editorPos = new Vector3(screenPosition.x * camStats.worldUnitsPerPixel, screenPosition.y * camStats.worldUnitsPerPixel, transform.localPosition.z);
            transform.localPosition = editorPos;
        }
    }
#endif
}
