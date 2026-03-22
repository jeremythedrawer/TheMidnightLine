using UnityEngine;
using static Atlas;
using static AtlasBatch;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class AtlasTextRenderer : MonoBehaviour
{
    public AtlasSO atlas;
    public CameraStatsSO camStats;
    public RenderInput uiRenderInstance;

    public Camera cam;
    public int depthOrder;

    [TextArea(3, 10)]
    public string text;
    public float kerning;
    public float spacing;

    [Header("Generated")]
    public Vector3 screenPosition;
    public float cursorX;

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
            //SetSprites();
        }
#endif
    }
    public void UpdateDepth(int newDepth)
    {
        depthOrder = newDepth;
        transform.position = new Vector3(transform.position.x, transform.position.y, depthOrder);
    }
    //private void SetSprites()
    //{
    //    uiRenderInstance.sprites = new SimpleSprite[text.Length];
    //    uiRenderInstance.matrices = new Matrix4x4[uiRenderInstance.sprites.Length];
    //    uiRenderInstance.widthHeightFlip = new Vector4[uiRenderInstance.sprites.Length];
    //    cursorX = 0;

    //    for (int i = 0; i < text.Length; i++)
    //    {
    //        char c = text[i];
    //        int asciiIndex = (int)c;
    //        if (asciiIndex < 33)
    //        {
    //            if (asciiIndex == 32)
    //            {
    //                cursorX += spacing;
    //            }
    //        }
    //        else
    //        {
    //            SimpleSprite sprite = atlas.simpleSprites[asciiIndex - 33];

    //            Vector2 spritePixelSize = sprite.worldSize * PIXELS_PER_UNIT;

    //            Vector3 worldOffset = new Vector3(cursorX * kerning, 0, 0);
    //            Vector3 matrixPos = transform.parent.position + ((screenPosition + worldOffset) * camStats.worldUnitsPerPixel);
    //            matrixPos.z = depthOrder;
    //            Vector3 matrixScale = new Vector3( spritePixelSize.x * camStats.worldUnitsPerPixel, spritePixelSize.y * camStats.worldUnitsPerPixel, 1f);

    //            uiRenderInstance.matrices[i] = Matrix4x4.TRS(matrixPos, transform.rotation, matrixScale);
    //            uiRenderInstance.sprites[i] = sprite;
    //            uiRenderInstance.widthHeightFlip[i] = Vector4.one;
    //            cursorX += spritePixelSize.x;
    //        }
    //    }
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
