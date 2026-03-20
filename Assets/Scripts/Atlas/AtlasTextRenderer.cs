using UnityEditor;
using UnityEngine;
using static Atlas;
using static AtlasBatch;

[ExecuteAlways]
public class AtlasTextRenderer : MonoBehaviour
{
    public AtlasSO atlas;
    public Material material;
    public CameraStatsSO camStats;
    public Camera cam;
    public SimpleSprite[] sprites;
    public int depthOrder;
    public Vector3 screenPosition;
    [TextArea(3, 10)]
    public string text;
    public float kerning;
    public float spacing;

    [Header("Generated")]
    public BatchKey batchKey;
    public MaterialPropertyBlock mpb;
    public Matrix4x4[] spriteMatrices;
    public float cursorX;
    public float worldUnitsPerPixel;

#if UNITY_EDITOR
    public Vector3 lastPos;
#endif
    private void OnValidate()
    {
        SetSprites();
    }
    private void OnEnable()
    {
        RegisterTextRenderer(this);
#if UNITY_EDITOR
        Selection.selectionChanged += UpdatePosition;
#endif
    }

#if UNITY_EDITOR
    private void Update()
    {
        if (!Application.isPlaying)
        {
            if (transform.position != lastPos)
            {
                screenPosition.x = transform.localPosition.x / worldUnitsPerPixel;
                screenPosition.y = transform.localPosition.y / worldUnitsPerPixel;
                lastPos = transform.position;
            }
            UpdateDepth((int)screenPosition.z);
            SetSprites();
        }
    }

    public void UpdatePosition()
    {
        if (Selection.activeGameObject == this.gameObject)
        {
            SetSprites();
            transform.localPosition = screenPosition * worldUnitsPerPixel;
        }
    }
#endif

    public void UpdateDepth(int newDepth)
    {
        depthOrder = newDepth;
        transform.position = new Vector3(transform.position.x, transform.position.y, depthOrder);
    }

    private void SetSprites()
    {
        int printableCharCount = 0;
        sprites = new SimpleSprite[text.Length];
        spriteMatrices = new Matrix4x4[sprites.Length];
        cursorX = 0;

        worldUnitsPerPixel = camStats.camHeight / Screen.height;

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
                SimpleSprite sprite = atlas.simpleSprites[asciiIndex - 33];

                Vector2 spritePixelSize = sprite.worldSize * PIXELS_PER_UNIT;

                float worldOffsetX = cursorX * kerning;
                Vector3 matrixPos = cam.transform.position + ((screenPosition + Vector3.right * worldOffsetX) * worldUnitsPerPixel);
                matrixPos.z = depthOrder;
                Vector3 matrixScale = new Vector3( spritePixelSize.x * worldUnitsPerPixel, spritePixelSize.y * worldUnitsPerPixel, 1f);

                spriteMatrices[i] = Matrix4x4.TRS(matrixPos, transform.rotation, matrixScale);
                sprites[i] = sprite;

                printableCharCount++;

                cursorX += spritePixelSize.x;
            }
        }
    }
}
