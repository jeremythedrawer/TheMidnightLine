using UnityEngine;
using UnityEngine.U2D;
using static Atlas;
using static AtlasBatch;

[ExecuteAlways]
public class AtlasTextRenderer : MonoBehaviour
{
    public AtlasSO atlas;
    public Material material;
    public SimpleSprite[] sprites;
    public int depthOrder;

    [TextArea(3, 10)]
    public string text;
    public float kerning;
    public float spacing;

    [Header("Generated")]
    public BatchKey batchKey;
    public MaterialPropertyBlock mpb;
    public Matrix4x4[] spriteMatrices;
    public float cursorX;
    public int printableCharacter;
    private void OnValidate()
    {
        SetSprites();
    }
    private void OnEnable()
    {
        RegisterTextRenderer(this);
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UpdateDepth((int)transform.position.z);
            SetSprites();
        }
#endif
    }
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
                Vector3 matrixPos = transform.position;
                matrixPos.x += cursorX * kerning;
                matrixPos.y -= sprite.uvPivot.y * sprite.worldSize.y;
                Vector3 matrixScale = new Vector3(sprite.worldSize.x, sprite.worldSize.y, 1f);

                spriteMatrices[i] = Matrix4x4.TRS(matrixPos, transform.rotation, matrixScale);
                sprites[i] = sprite;
                printableCharCount++;
                cursorX += sprite.worldSize.x;
            }
        }
    }
}
