using UnityEngine;
using static Atlas;
using static AtlasRendering;
[ExecuteAlways]
public class AtlasUISimpleRenderer : AtlasRenderer
{
    public SingularRenderInput renderInput;
    public CameraStatsSO camStats;

    [Header("UI Simple (User)")]
    public float width;
    public float height;
    public int spriteIndex;
    [Header("Simple (Generated)")]
    public SimpleSprite sprite;

    private void Awake()
    {
        renderInput.InitRenderer(gameObject);
    }

    private void OnValidate()
    {
        renderInput.InitRenderer(gameObject);
        spriteIndex = Mathf.Clamp(spriteIndex, 0, renderInput.atlas.simpleSprites.Length - 1);
        sprite = renderInput.atlas.simpleSprites[spriteIndex];

        renderInput.UpdateRenderInputsScreen(width, height, sprite, camStats);
    }
    private void OnEnable()
    {
        renderInput.InitRenderer(gameObject);
        RegisterSingleRenderInput(renderInput);
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
            renderInput.UpdateDepthEditor(transform);
            renderInput.UpdateRenderInputsScreen(width, height, sprite, camStats);
        }
#endif

        if (transform.hasChanged)
        {
            renderInput.bounds.center = new Vector3(transform.position.x + renderInput.boundsOffset.x, transform.position.y + renderInput.boundsOffset.y, transform.position.z);
        }
    }

    public void SetSprite(SimpleSprite sprite)
    {
        renderInput.UpdateRenderInputsScreen(width, height, sprite, camStats);
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
