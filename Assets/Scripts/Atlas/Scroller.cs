using UnityEngine;
using static Atlas;
using static AtlasRendering;
public class Scroller : MonoBehaviour
{
    public AtlasRenderer atlasRenderer;
    public ZoneSpawnerSO spawner;
    public TrainStatsSO trainStats;
    public ScrollSprite scrollSprite;
    public int activeScrollerIndex;
    public void InitScroll(ref ScrollSprite scrollSpriteInput, int activeIndex)
    {
        scrollSprite = scrollSpriteInput;
        atlasRenderer.rendererType = scrollSprite.rendererType;
        atlasRenderer.batchKey.material = scrollSprite.material;
        atlasRenderer.batchKey.depthOrder = scrollSprite.depth;
        atlasRenderer.atlas = scrollSprite.atlas;

        activeScrollerIndex = activeIndex;

        transform.position = new Vector3(spawner.bounds.max.x, 0, scrollSprite.depth);

        switch (scrollSprite.rendererType)
        {
            case AtlasRendererType.SimpleWorld:
            {
                SimpleSprite sprite = atlasRenderer.atlas.simpleSprites[scrollSprite.spriteIndex];
                atlasRenderer.SetWidthFromWorldSpace(spawner.bounds.size.x, ref sprite);
                atlasRenderer.custom.x = trainStats.metersTravelled;
            }
            break;
            case AtlasRendererType.SliceWorld:
            {
                SliceSprite sliceSprite = atlasRenderer.atlas.slicedSprites[scrollSprite.spriteIndex];
                atlasRenderer.SetNineSliceWidthFromWorldSpace(spawner.bounds.size.x, ref sliceSprite);

                for (int i = 0; i < 9; i++)
                {
                    atlasRenderer.customs[i].x = trainStats.metersTravelled;
                }
            }
            break;
        }
    }

    public void ScrollAway()
    {
        switch (scrollSprite.rendererType)
        {
            case AtlasRendererType.SimpleWorld:
            {
                atlasRenderer.custom.x = trainStats.metersTravelled;
            }
            break;
            case AtlasRendererType.SliceWorld:
            {
                for (int i = 0; i < 9; i++)
                {
                    atlasRenderer.customs[i].x = trainStats.metersTravelled;
                }
            }
            break;
        }
    }

    public void CheckToDeactivate()
    {
        switch (scrollSprite.rendererType)
        {
            case AtlasRendererType.SimpleWorld:
            {
                if ((trainStats.metersTravelled - atlasRenderer.custom.x) < spawner.bounds.size.x)
                {
                    ScrollSpawner.ReturnScroller(this);
                }
            }
            break;
            case AtlasRendererType.SliceWorld:
            {
                if ((trainStats.metersTravelled - atlasRenderer.customs[0].x) < spawner.bounds.size.x)
                {
                    ScrollSpawner.ReturnScroller(this);
                }
            }
            break;
        }
    }

}
