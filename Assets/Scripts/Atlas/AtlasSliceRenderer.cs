using UnityEngine;
using static Atlas;
using static AtlasBatch;

[ExecuteAlways]
public class AtlasSliceRenderer : MonoBehaviour
{
    public MultipleRenderInput renderInput;
    public int spriteIndex;
    public float width = 1;
    public float height = 1;
    [Header("Generated")]
    public SliceSprite slicedSprite;
    public int quadCount;

    private void Awake()
    {
        if(renderInput == null) return;
        renderInput.InitRenderer(gameObject);
    }

    private void OnValidate()
    {
        if (renderInput == null || renderInput.atlas == null) return;
        spriteIndex = Mathf.Clamp(spriteIndex, 0, renderInput.atlas.slicedSprites.Length - 1);
        slicedSprite = renderInput.atlas.slicedSprites[spriteIndex];
        renderInput.UpdateSlicedSprite(slicedSprite, width, height);
    }
    private void OnEnable()
    {
        if(renderInput == null) return;
        renderInput.InitRenderer(gameObject);
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
            if (renderInput == null || renderInput.atlas == null) return;
            renderInput.UpdateDepth((int)transform.position.z);
        }
#endif

        if (transform.hasChanged)
        {
            renderInput.bounds.center = new Vector3(transform.position.x + renderInput.boundsOffset.x, transform.position.y + renderInput.boundsOffset.y, transform.position.z);
        }
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
