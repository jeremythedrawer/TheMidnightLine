using UnityEngine;
using UnityEngine.VFX;
using static Atlas;
using static AtlasBatch;
public class GlyphSetter : MonoBehaviour
{
    public int spriteIndex;
    public float widthMax;
    public float widthMin;
    public float width;

    public float heightMax;
    public float heightMin;
    public float height;

    public AtlasSO glyphAtlas;
    public VisualEffect vfx;
    public GraphicsBuffer uvSizeAndPosBuffer;
    public GraphicsBuffer worldSizeAndPosBuffer;
    public GraphicsBuffer widthHeightBuffer;

    public float lifetime;
    public float deltaTime;
    private void OnValidate()
    {
        if (uvSizeAndPosBuffer == null || worldSizeAndPosBuffer == null || widthHeightBuffer == null) return;
        UpdateVFX();
    }

    private void Start()
    {
        deltaTime += 0.1f;
        CreateBuffers();
        GetLifetime();
    }

    private void OnDisable()
    {
        ReleaseBuffers();
    }
    private void OnDestroy()
    {
        ReleaseBuffers();
    }
    private void Update()
    {
        deltaTime += Time.deltaTime;

        if (deltaTime > lifetime * 2)
        {
            width = Random.Range(widthMin, widthMax);
            height = Random.Range(heightMin, heightMax);
            UpdateVFX();
            deltaTime = 0;
        }
    }
    private void CreateBuffers()
    {
        uvSizeAndPosBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 9, sizeof(float) * 4);
        worldSizeAndPosBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 9, sizeof(float) * 4);
        widthHeightBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 9, sizeof(float) * 2);
    }
    private void GetLifetime()
    {
        lifetime = vfx.GetFloat("Lifetime");
    }
    private void UpdateVFX()
    {
        Vector4[] uvSizeAndPosArray = new Vector4[9];

        SliceSprite sliceSprite = glyphAtlas.slicedSprites[spriteIndex];
        for (int i = 0; i < 9; i++)
        {
            uvSizeAndPosArray[i] = sliceSprite.uvSizeAndPos[i];
        }

        float worldWidthSize = sliceSprite.sprite.worldSize.x * width;
        float worldHeightSize = sliceSprite.sprite.worldSize.y * height;

        float rightPos = worldWidthSize - sliceSprite.worldSlices.y;
        float topPos = worldHeightSize - sliceSprite.worldSlices.w;
        float centerWidth = rightPos - sliceSprite.worldSlices.x;
        float centerHeight = topPos - sliceSprite.worldSlices.z;

        Vector4[] worldSizeAndPosArray = new Vector4[]
        {
            new Vector4(sliceSprite.worldSlices.x,  sliceSprite.worldSlices.z, 0, 0),
            new Vector4(centerWidth, sliceSprite.worldSlices.z, sliceSprite.worldSlices.x, 0),
            new Vector4(sliceSprite.worldSlices.y, sliceSprite.worldSlices.z, rightPos, 0),

            new Vector4(sliceSprite.worldSlices.x, centerHeight, 0, sliceSprite.worldSlices.z),
            new Vector4(centerWidth, centerHeight, sliceSprite.worldSlices.x, sliceSprite.worldSlices.z),
            new Vector4(sliceSprite.worldSlices.y, centerHeight, rightPos, sliceSprite.worldSlices.z),

            new Vector4(sliceSprite.worldSlices.x, sliceSprite.worldSlices.w, 0, topPos),
            new Vector4(centerWidth, sliceSprite.worldSlices.w, sliceSprite.worldSlices.x, topPos),
            new Vector4(sliceSprite.worldSlices.y, sliceSprite.worldSlices.w, rightPos, topPos),
        };

        float originalCenterWidth = sliceSprite.sprite.worldSize.x - sliceSprite.worldSlices.x - sliceSprite.worldSlices.y;

        float originalCenterHeight = sliceSprite.sprite.worldSize.y - sliceSprite.worldSlices.z - sliceSprite.worldSlices.w;
        float uvCenterWidth = centerWidth / originalCenterWidth;
        float uvCenterHeight = centerHeight / originalCenterHeight;

        Vector2[] widthHeightArray = new Vector2[]
        {
            new Vector2(1, 1),
            new Vector2(uvCenterWidth, 1),
            new Vector2(1, 1),
            new Vector2(1, uvCenterHeight),
            new Vector2(uvCenterWidth, uvCenterHeight),
            new Vector2(1, uvCenterHeight),
            new Vector2(1, 1),
            new Vector2(uvCenterWidth, 1),
            new Vector2(1, 1),
        };


        uvSizeAndPosBuffer.SetData(uvSizeAndPosArray);
        worldSizeAndPosBuffer.SetData(worldSizeAndPosArray);
        widthHeightBuffer.SetData(widthHeightArray);


        float lineHeight = vfx.GetFloat("LineHeight");
        float lineSpace = vfx.GetFloat("LineSpace");

        float lineSegment = lineHeight + (lineSpace * ( 1- (lineHeight / lineSpace)));

        int lineCount = Mathf.CeilToInt(centerHeight / lineSegment);

        Vector2 linePosition = new Vector2(sliceSprite.worldSlices.x, topPos - lineHeight);

        vfx.SetGraphicsBuffer("_UVSizeAndPos", uvSizeAndPosBuffer);
        vfx.SetGraphicsBuffer("_WorldSizeAndPos", worldSizeAndPosBuffer);
        vfx.SetGraphicsBuffer("_WidthHeight", widthHeightBuffer);
        vfx.SetFloat("_LineWidth", centerWidth);
        vfx.SetInt("_LineCount", lineCount);
        vfx.SetVector2("_LinePosition", linePosition);

        lifetime = (width + lineCount) * 0.4f;
        vfx.SetFloat("Lifetime", lifetime);

        vfx.SetMesh("_Quad", GetQuad());
    }

    private void ReleaseBuffers()
    {
        uvSizeAndPosBuffer?.Release();
        worldSizeAndPosBuffer?.Release();
        widthHeightBuffer?.Release();
        uvSizeAndPosBuffer = null;
        worldSizeAndPosBuffer = null;
        widthHeightBuffer = null;
    }
}
