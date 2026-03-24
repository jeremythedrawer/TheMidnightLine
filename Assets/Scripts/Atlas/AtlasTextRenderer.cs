using UnityEngine;
using static Atlas;
using static AtlasRendering;
using UnityEngine.U2D;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class AtlasTextRenderer : AtlasRenderer
{
    public MultipleRenderInput renderInput;
    public CameraStatsSO camStats;

    public Camera cam;
    public int depthOrder;

    [TextArea(3, 10)]
    public string text;
    public float kerning;
    public float spacing;

    [Header("Generated")]
    public Vector3 screenPosition;
    public float cursorX;
    public SimpleSprite[] sprites;
    private void Awake()
    {
        renderInput.InitRenderer(gameObject);
    }

    private void OnValidate()
    {
        SetText();
    }
    private void OnEnable()
    {
        renderInput.InitRenderer(gameObject);
        RegisterMultipleRenderInput(renderInput);
        SetText();
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
            renderInput.UpdateDepth((int)transform.position.z);
        }
#endif
    }
    private void SetText()
    {

        cursorX = 0;
        int printableChars = 0;

        for (int i = 0; i < text.Length; i++)
        {
            int asciiIndex = (int)text[i];
            if (asciiIndex >= 33)
            {
                printableChars++;
            }
        }

        sprites = new SimpleSprite[printableChars];
        renderInput.pivotAndSize = new Vector4[printableChars];
        renderInput.uvSizeAndPos = new Vector4[printableChars];
        renderInput.scaleAndFlip = new Vector4[printableChars];

        int spriteIndex = 0;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            int asciiIndex = (int)c;
            if (asciiIndex < 33)
            {
                if (asciiIndex == 32)
                {
                    cursorX += spacing;
                }
            }
            else
            {
                SimpleSprite sprite = renderInput.atlas.simpleSprites[asciiIndex - 33];
                Vector2 spritePixelSize = sprite.worldSize * PIXELS_PER_UNIT;

                Vector3 worldOffset = new Vector3(cursorX * kerning, 0, 0);
                Vector2 matrixPos = worldOffset * camStats.worldUnitsPerPixel;
                ref Vector4 worldPivotAndSize = ref renderInput.pivotAndSize[spriteIndex];
                worldPivotAndSize.x = -matrixPos.x;
                worldPivotAndSize.y = -matrixPos.y;
                worldPivotAndSize.z = spritePixelSize.x * camStats.worldUnitsPerPixel;
                worldPivotAndSize.w = spritePixelSize.y * camStats.worldUnitsPerPixel;

                renderInput.uvSizeAndPos[spriteIndex] = sprite.uvSizeAndPos;
                renderInput.scaleAndFlip[spriteIndex] = Vector4.one;
                cursorX += spritePixelSize.x;
                spriteIndex++;
            }
        }
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
