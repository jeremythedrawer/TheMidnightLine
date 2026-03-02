using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.VFX;
using static Atlas;
using static AtlasBatch;
public class GlyphSetter : MonoBehaviour
{
    public int spriteIndex;
    public float width;
    public float height;

    public AtlasSO glyphAtlas;
    public VisualEffect vfx;
    public GraphicsBuffer uvSizeAndPosBuffer;
    public GraphicsBuffer worldSizeAndPosBuffer;
    public GraphicsBuffer widthHeightBuffer;
    private void OnValidate()
    {
        uvSizeAndPosBuffer?.Release();
        worldSizeAndPosBuffer?.Release();
        widthHeightBuffer?.Release();
        uvSizeAndPosBuffer = null;
        worldSizeAndPosBuffer = null;
        widthHeightBuffer = null;

        uvSizeAndPosBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 9, sizeof(float) * 4);
        worldSizeAndPosBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 9, sizeof(float) * 4);
        widthHeightBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 9, sizeof(float) * 2);

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
            new Vector4(sliceSprite.worldSlices.x,  sliceSprite.worldSlices.z,  0,                          0),
            new Vector4(centerWidth,                sliceSprite.worldSlices.z,  sliceSprite.worldSlices.x,  0),
            new Vector4(sliceSprite.worldSlices.y,  sliceSprite.worldSlices.z,  rightPos,  0),
            
            new Vector4(sliceSprite.worldSlices.x,  centerHeight,               0,                          sliceSprite.worldSlices.z),
            new Vector4(centerWidth,                centerHeight,               sliceSprite.worldSlices.x,  sliceSprite.worldSlices.z),
            new Vector4(sliceSprite.worldSlices.y,  centerHeight,               rightPos,                   sliceSprite.worldSlices.z),

            new Vector4(sliceSprite.worldSlices.x,  sliceSprite.worldSlices.w,  0,                          topPos),
            new Vector4(centerWidth,                sliceSprite.worldSlices.w,  sliceSprite.worldSlices.x,  topPos),
            new Vector4(sliceSprite.worldSlices.y,  sliceSprite.worldSlices.w,  rightPos,                   topPos),
        };

        float originalCenterWidth = sliceSprite.sprite.worldSize.x - sliceSprite.worldSlices.x - sliceSprite.worldSlices.y;

        float originalCenterHeight = sliceSprite.sprite.worldSize.y - sliceSprite.worldSlices.z - sliceSprite.worldSlices.w;
        float uvCenterWidth = centerWidth / originalCenterWidth;
        float uvCenterHeight = centerHeight / originalCenterHeight;

        Vector2[] widthHeight = new Vector2[]
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
        widthHeightBuffer.SetData(widthHeight);

        vfx.SetGraphicsBuffer("_UVSizeAndPos", uvSizeAndPosBuffer);
        vfx.SetGraphicsBuffer("_WorldSizeAndPos", worldSizeAndPosBuffer);
        vfx.SetGraphicsBuffer("_WidthHeight", widthHeightBuffer);
        vfx.SetMesh("_Quad", GetQuad());
    }
}
