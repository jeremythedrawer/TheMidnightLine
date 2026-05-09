using UnityEngine;
using static Atlas;
using static AtlasRendering;
using static AtlasSpawn;
[CreateAssetMenu(fileName = "ParticleAtlas", menuName = "Atlas / Particle Atlas")]
public class ParticleAtlas : ScriptableObject
{
    public AtlasSO atlas;
    public ParticleType particleType;

    [Header("Trip Editor")]
    public ParticlePosData[] posData;
    
    public int posDataIndexOffset;
    public int spriteCount;
    public bool isCompleted;

    [Header("Generated")]
    public ParticleSpriteData[] spriteData;
    public GraphicsBuffer spriteDataBuffer;

    private void OnValidate()
    {
        if (atlas == null)
        {
            spriteData = null;
            return;
        }

            float width = 1;
        float height = 1;

        int totalSprites = atlas.simpleSprites.Length + (atlas.slicedSprites.Length * 9);
        ParticleSpriteData[] newSpriteDataArray = new ParticleSpriteData[totalSprites];

        int spriteIndex = 0;
        for (int i = 0; i < atlas.simpleSprites.Length; i++)
        {
            ParticleSpriteData newSpriteData = new ParticleSpriteData();
            SimpleSprite sprite = atlas.simpleSprites[i];
            newSpriteData.uvSizeAndPos = sprite.uvSizeAndPos;
            newSpriteData.worldPivotAndSize = new Vector4(0, 0, sprite.worldSize.x, sprite.worldSize.y);
            newSpriteData.scaleAndFlip = new Vector4(width, height, 1, 1);
            newSpriteDataArray[spriteIndex++] = newSpriteData;
        }



        Vector4 scaleFlipCQuad = Vector4.one;
        Vector4 scaleFlipHQuad = new Vector4(width, 1, 1, 1);
        Vector4 scaleFlipVQuad = new Vector4(1, height, 1, 1);
        Vector4 scaleFlipMQuad = new Vector4(width, height, 1, 1);

        Vector4[] scalesAndFlips = new Vector4[]
        {
            scaleFlipCQuad,
            scaleFlipHQuad,
            scaleFlipCQuad,

            scaleFlipVQuad,
            scaleFlipMQuad,
            scaleFlipVQuad,

            scaleFlipCQuad,
            scaleFlipHQuad,
            scaleFlipCQuad
        };
        for (int i = 0; i < atlas.slicedSprites.Length; i++)
        {
            SliceSprite sliceSprite = atlas.slicedSprites[i];

            float centerWorldSliceWidth = sliceSprite.sprite.worldSize.x - sliceSprite.worldSlices.x - sliceSprite.worldSlices.y;
            float centerWorldSliceHeight = sliceSprite.sprite.worldSize.y - sliceSprite.worldSlices.z - sliceSprite.worldSlices.w;

            float rightColPos = sliceSprite.worldSlices.x + (centerWorldSliceWidth * width);
            float topRowPos = sliceSprite.worldSlices.z + (centerWorldSliceHeight * height);

            Vector4[] worldPivotsAndSizes = new Vector4[]
            {
                new Vector4(0, 0, sliceSprite.worldSlices.x, sliceSprite.worldSlices.z),
                new Vector4(-sliceSprite.worldSlices.x, 0, centerWorldSliceWidth, sliceSprite.worldSlices.z),
                new Vector4(-rightColPos, 0, sliceSprite.worldSlices.y, sliceSprite.worldSlices.z),

                new Vector4(0, -sliceSprite.worldSlices.z, sliceSprite.worldSlices.x, centerWorldSliceHeight),
                new Vector4(-sliceSprite.worldSlices.x, -sliceSprite.worldSlices.z, centerWorldSliceWidth, centerWorldSliceHeight),
                new Vector4(-rightColPos, -sliceSprite.worldSlices.z, sliceSprite.worldSlices.y, centerWorldSliceHeight),

                new Vector4(0, -topRowPos, sliceSprite.worldSlices.x, sliceSprite.worldSlices.w),
                new Vector4(-sliceSprite.worldSlices.x, -topRowPos, centerWorldSliceWidth, sliceSprite.worldSlices.w),
                new Vector4(-rightColPos, -topRowPos, sliceSprite.worldSlices.y, sliceSprite.worldSlices.w),
            };

            for (int j = 0; j < 9; j++)
            {
                ParticleSpriteData newSpriteData = new ParticleSpriteData();
                newSpriteData.uvSizeAndPos = sliceSprite.uvSizeAndPos[j];
                newSpriteData.worldPivotAndSize = worldPivotsAndSizes[j];
                newSpriteData.scaleAndFlip = scalesAndFlips[j];
                newSpriteDataArray[spriteIndex++] = newSpriteData;
            }
        }

        spriteData = newSpriteDataArray;
    }
}
