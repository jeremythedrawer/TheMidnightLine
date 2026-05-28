using UnityEditor;
using UnityEngine;
using static Atlas;
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
    public ParticleSpritesData[] spriteData;
    public GraphicsBuffer spriteDataBuffer;
    public void SetSpriteData()
    {
        if (atlas == null)
        {
            spriteData = null;
            return;
        }
        float width = 1;
        float height = 1;

        int totalSprites = atlas.simpleSprites.Length + (atlas.slicedSprites.Length * 9);
        ParticleSpritesData[] newSpriteDataArray = new ParticleSpritesData[totalSprites];

        int spriteIndex = 0;

        spriteCount = 0;
        for (int i = 0; i < atlas.simpleSprites.Length; i++)
        {
            ParticleSpritesData newSpriteData = new ParticleSpritesData();
            SimpleSprite sprite = atlas.simpleSprites[i];
            newSpriteData.uvSizeAndPos = sprite.uvSizeAndPos;

            float pivotX = -sprite.uvPivot.x * sprite.worldSize.x;
            float pivotY = -sprite.uvPivot.y * sprite.worldSize.y;
            newSpriteData.worldPivotAndSize = new Vector4(pivotX, pivotY, sprite.worldSize.x, sprite.worldSize.y);
            newSpriteDataArray[spriteIndex] = newSpriteData;
            spriteIndex++;
            spriteCount++;
        }


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
                new Vector4(sliceSprite.worldSlices.x, 0, centerWorldSliceWidth, sliceSprite.worldSlices.z),
                new Vector4(rightColPos, 0, sliceSprite.worldSlices.y, sliceSprite.worldSlices.z),

                new Vector4(0, sliceSprite.worldSlices.z, sliceSprite.worldSlices.x, centerWorldSliceHeight),
                new Vector4(sliceSprite.worldSlices.x, sliceSprite.worldSlices.z, centerWorldSliceWidth, centerWorldSliceHeight),
                new Vector4(rightColPos, sliceSprite.worldSlices.z, sliceSprite.worldSlices.y, centerWorldSliceHeight),

                new Vector4(0, topRowPos, sliceSprite.worldSlices.x, sliceSprite.worldSlices.w),
                new Vector4(sliceSprite.worldSlices.x, topRowPos, centerWorldSliceWidth, sliceSprite.worldSlices.w),
                new Vector4(rightColPos, topRowPos, sliceSprite.worldSlices.y, sliceSprite.worldSlices.w),
            };

            for (int j = 0; j < 9; j++)
            {
                ParticleSpritesData newSpriteData = new ParticleSpritesData();
                newSpriteData.uvSizeAndPos = sliceSprite.uvSizeAndPos[j];
                newSpriteData.worldPivotAndSize = worldPivotsAndSizes[j];
                newSpriteDataArray[spriteIndex] = newSpriteData;
                spriteIndex++;
            }
            spriteCount++;
        }

        spriteData = newSpriteDataArray;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(ParticleAtlas))]
public class ParticleAtlasEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        ParticleAtlas particleAtlas = (ParticleAtlas)target;

        if (GUILayout.Button("Set Sprite Data"))
        {
            particleAtlas.SetSpriteData();

            EditorUtility.SetDirty(particleAtlas);
        }
    }
}

#endif