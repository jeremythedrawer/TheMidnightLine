using UnityEngine;
using UnityEngine.UIElements;
using static Atlas;
using static AtlasRendering;
using static AtlasSpawn;
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
            SetState(ScrollState.MovingOut);
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
                    }
                    break;
                    case ScrollSpriteType.Sliced:
                    {
                        atlasRenderer.rendererType = AtlasRendererType.SliceWorld;
                        SliceSprite sliceSprite = atlasRenderer.atlas.slicedSprites[scrollSprite.spriteIndex];
                        atlasRenderer.SetNineSliceWidthFromWorldSpace(spawner.bounds.size.x, ref sliceSprite);

                        for (int i = 0; i < 9; i++)
                        {
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
                    }
                    break;
                    case ScrollSpriteType.Sliced:
                    {
                        for (int i = 0; i < 9; i++)
                        {
                        }
                    }
                    break;
                }
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


        switch (scrollSprite.scrollType)
        {
            case ScrollSpriteType.Simple:
            {
                transform.position = new Vector3(spawner.bounds.max.x, 0, scrollSprite.depth);
                atlasRenderer.rendererType = AtlasRendererType.SimpleWorld;
                SimpleSprite sprite = atlasRenderer.atlas.simpleSprites[scrollSprite.spriteIndex];
                atlasRenderer.UpdateSpriteInputs(ref sprite);
            }
            break;
            case ScrollSpriteType.Tiled:
            {
                transform.position = new Vector3(spawner.bounds.max.x, scrollSprite.height, scrollSprite.depth);
                atlasRenderer.rendererType = AtlasRendererType.SimpleWorld;
                SimpleSprite sprite = atlasRenderer.atlas.simpleSprites[scrollSprite.spriteIndex];
                atlasRenderer.SetWidthFromWorldSpace(spawner.bounds.size.x, ref sprite);

                atlasRenderer.custom.x = atlasRenderer.batchKey.depthOrder;

                spawner.scrollMoveInputBuffer.GetData(spawner.moveInputs);
                spawner.moveInputs[atlasRenderer.batchKey.depthOrder] = 1;
                spawner.scrollMoveInputBuffer.SetData(spawner.moveInputs);
                spawner.scrollCompute.Dispatch(spawner.scrollKernelUpdate, spawner.scrollComputeGroupSize, 1, 1);
            }
            break;
            case ScrollSpriteType.Sliced:
            {
                atlasRenderer.rendererType = AtlasRendererType.SliceWorld;
                SliceSprite sliceSprite = atlasRenderer.atlas.slicedSprites[scrollSprite.spriteIndex];
                transform.position = new Vector3(spawner.bounds.max.x - sliceSprite.worldSlices.y, scrollSprite.height, scrollSprite.depth);
                atlasRenderer.SetNineSliceWidthFromWorldSpace(spawner.bounds.size.x, ref sliceSprite);

                for (int i = 0; i < 9; i++)
                {
                    atlasRenderer.customs[i].x = atlasRenderer.batchKey.depthOrder;
                }
                spawner.scrollMoveInputBuffer.GetData(spawner.moveInputs);
                spawner.moveInputs[atlasRenderer.batchKey.depthOrder] = 1;
                spawner.scrollMoveInputBuffer.SetData(spawner.moveInputs);
                spawner.scrollCompute.Dispatch(spawner.scrollKernelUpdate, spawner.scrollComputeGroupSize, 1, 1);
            }
            break;
        }

        gameObject.SetActive(true);
    }
}
