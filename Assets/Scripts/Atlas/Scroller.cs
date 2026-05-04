using UnityEngine;
using static Atlas;
using static AtlasSpawn;
using static AtlasRendering;
public class Scroller : MonoBehaviour
{
    public TrainStatsSO trainStats;
    public SpawnSO spawner;
    public SpyStatsSO spyStats;
    public AtlasRenderer atlasRenderer;
    [Header("Generated")]
    public ScrollSprite scrollSprite;
    public int activeScrollerIndex;
    public float metersTravelledAtMoveOut;
    public ScrollState state;

    private void Update()
    {
        ChooseState();
    }

    public void ChooseState()
    {
        if (spyStats.ticketsCheckedTotal >= scrollSprite.ticketCheckEnd)
        {
            if ((trainStats.metersTravelled - metersTravelledAtMoveOut) > spawner.bounds.size.x)
            {
                SetState(ScrollState.Dead);
            }
            else
            {
               SetState(ScrollState.MovingOut);
            }

        }
        else if (spyStats.ticketsCheckedTotal >= scrollSprite.ticketCheckStart)
        {
            SetState(ScrollState.MovingIn);
        }
    }
    public void SetState(ScrollState newState)
    {
        if (newState == state) return;
        ExitState();
        state = newState;
        EnterState();
    }
    public void EnterState()
    {
        switch(state)
        {
            case ScrollState.MovingIn:
            {
                switch (scrollSprite.scrollType)
                {
                    case ScrollSpriteType.Simple:
                    {
                    }
                    break;
                    case ScrollSpriteType.Tiled:
                    {
                        atlasRenderer.rendererType = AtlasRendererType.SimpleWorld;
                        SimpleSprite sprite = atlasRenderer.atlas.simpleSprites[scrollSprite.spriteIndex];
                        atlasRenderer.SetWidthFromWorldSpace(spawner.bounds.size.x, ref sprite);
                        atlasRenderer.custom.x = trainStats.metersTravelled;
                        atlasRenderer.custom.y = 1;
                    }
                    break;
                    case ScrollSpriteType.Sliced:
                    {
                        atlasRenderer.rendererType = AtlasRendererType.SliceWorld;
                        SliceSprite sliceSprite = atlasRenderer.atlas.slicedSprites[scrollSprite.spriteIndex];
                        atlasRenderer.SetNineSliceWidthFromWorldSpace(spawner.bounds.size.x, ref sliceSprite);

                        for (int i = 0; i < 9; i++)
                        {
                            atlasRenderer.customs[i].x = trainStats.metersTravelled;
                            atlasRenderer.custom.y = 1;
                        }
                    }
                    break;
                }

                metersTravelledAtMoveOut = float.MaxValue;
            }
            break;
            case ScrollState.MovingOut:
            {
                switch (scrollSprite.scrollType)
                {
                    case ScrollSpriteType.Simple:
                    {
                    }
                    break;
                    case ScrollSpriteType.Tiled:
                    {
                        atlasRenderer.custom.x = trainStats.metersTravelled - spawner.bounds.size.x;
                        atlasRenderer.custom.y = 2;
                    }
                    break;
                    case ScrollSpriteType.Sliced:
                    {
                        for (int i = 0; i < 9; i++)
                        {
                            atlasRenderer.custom.x = trainStats.metersTravelled - spawner.bounds.size.x;
                            atlasRenderer.custom.y = 2;
                        }
                    }
                    break;
                }
                metersTravelledAtMoveOut = trainStats.metersTravelled;
            }
            break;
            case ScrollState.Dead:
            {
            }
            break;
        }
    }
    public void ExitState()
    {
        switch (state)
        {
            case ScrollState.MovingIn:
            {

            }
            break;
            case ScrollState.MovingOut:
            {

            }
            break;
        }
    }
    public void InitScroll(ScrollSprite scrollSpriteInput, int activeIndex)
    {
        scrollSprite = scrollSpriteInput;
        atlasRenderer.batchKey.depthOrder = scrollSprite.depth;
        atlasRenderer.atlas = scrollSprite.atlas;

        activeScrollerIndex = activeIndex;

        transform.position = new Vector3(spawner.bounds.max.x, 0, scrollSprite.depth);

        switch (scrollSprite.scrollType)
        {
            case ScrollSpriteType.Simple:
            {
                atlasRenderer.rendererType = AtlasRendererType.SimpleWorld;
                SimpleSprite sprite = atlasRenderer.atlas.simpleSprites[scrollSprite.spriteIndex];
                atlasRenderer.UpdateSpriteInputs(ref sprite);
                atlasRenderer.custom.x = trainStats.metersTravelled;
            }
            break;
            case ScrollSpriteType.Tiled:
            {
                atlasRenderer.rendererType = AtlasRendererType.SimpleWorld;
                SimpleSprite sprite = atlasRenderer.atlas.simpleSprites[scrollSprite.spriteIndex];
                atlasRenderer.SetWidthFromWorldSpace(spawner.bounds.size.x, ref sprite);
                atlasRenderer.custom.x = trainStats.metersTravelled;
            }
            break;
            case ScrollSpriteType.Sliced:
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
}
