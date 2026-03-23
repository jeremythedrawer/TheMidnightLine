using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.U2D;
using static Atlas;
using static AtlasBatch;

[ExecuteAlways]
public class AtlasSimpleRenderer : MonoBehaviour
{
    public SingularRenderInput renderInput;

    [Header("Simple (User)")]
    public float width = 1;
    public float height = 1;
    public int spriteIndex;
    [Header("Simple (Generated)")]
    public SimpleSprite sprite;
    private void OnValidate()
    {
        if (renderInput == null || renderInput.atlas == null) return;
        spriteIndex = Mathf.Clamp(spriteIndex, 0, renderInput.atlas.simpleSprites.Length - 1);
        sprite = renderInput.atlas.simpleSprites[spriteIndex];
        renderInput.UpdateRenderInputsWorld(width, height, sprite);
    }
    private void OnEnable()
    {
        if (renderInput == null || renderInput.atlas == null) return;
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
    private void Start()
    {
        renderInput.InitRenderer(gameObject);
        renderInput.UpdateDepth(renderInput.batchKey.depthOrder);
    }
    private void Update()
    {
#if UNITY_EDITOR
        if(!Application.isPlaying)
        {
            renderInput.UpdateDepth((int)transform.position.z);
        }
#endif
        renderInput.UpdateBounds();
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
