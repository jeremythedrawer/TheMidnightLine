using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using static Atlas;
using static AtlasBatch;
[ExecuteAlways]
public class AtlasUIRenderer : MonoBehaviour
{
    public AtlasSO atlas;
    public CameraStatsSO camStats;
    public RenderInput uiRenderInstance;

    public int spriteIndex;
    public float width;
    public float height;
    public bool flipX;
    public bool flipY;

    [Header("Generated")]
    public Vector3 screenPosition;

#if UNITY_EDITOR
    [Header("Editor")]
    public Vector3 lastPos;
#endif
    private void OnEnable()
    {
        RegisterUIRenderer(uiRenderInstance);
#if UNITY_EDITOR
        Selection.selectionChanged += UpdateEditorPosition;
#endif
    }

    private void OnDisable()
    {
        UnregisterUIRenderer(uiRenderInstance);
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            if (transform.position != lastPos)
            {
                screenPosition.x = transform.localPosition.x / camStats.worldUnitsPerPixel;
                screenPosition.y = transform.localPosition.y / camStats.worldUnitsPerPixel;
                lastPos = transform.position;
            }
            UpdateDepth((int)transform.position.z);
            //SetSprite();
        }
#endif
    }
    public void UpdateDepth(int newDepth)
    {
        uiRenderInstance.batchKey.depthOrder = newDepth;
        transform.position = new Vector3(transform.position.x, transform.position.y, uiRenderInstance.batchKey.depthOrder);
    }
    //private void SetSprite()
    //{
    //    uiRenderInstance.sprites = new SimpleSprite[1];
    //    uiRenderInstance.matrices = new Matrix4x4[1];

    //    spriteIndex = Mathf.Clamp(spriteIndex, 0, atlas.simpleSprites.Length -  1);
    //    SimpleSprite sprite = atlas.simpleSprites[spriteIndex];

    //    Vector2 spritePixelSize = sprite.worldSize * PIXELS_PER_UNIT;

    //    Vector3 matrixPos = transform.parent.position + (screenPosition * camStats.worldUnitsPerPixel);
    //    matrixPos.z = uiRenderInstance.batchKey.depthOrder;
    //    Vector3 matrixScale = new Vector3(spritePixelSize.x * camStats.worldUnitsPerPixel, spritePixelSize.y * camStats.worldUnitsPerPixel, 1f);

    //    uiRenderInstance.sprites[0] = sprite;
    //    uiRenderInstance.matrices[0] = Matrix4x4.TRS(matrixPos, transform.rotation, matrixScale);
    //    uiRenderInstance.gameObject = gameObject;
    //    uiRenderInstance.widthHeightFlip = new Vector4[] { new Vector4(width, height, flipX ? -1 : 1, flipY ? -1 : 1) };
    //}

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
