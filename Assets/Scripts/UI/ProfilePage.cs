using UnityEngine;
using static AtlasUI;
using static Passenger;

public class ProfilePage : MonoBehaviour
{
    public Page page;

    public PassengersData passengersData;

    public AtlasRenderer coveredMugShotRenderer;
    public AtlasRenderer uncoveredMugShotRenderer;
    public AtlasTextRenderer[] habitTextRenderers;

    [Header("Generated")]
    public int traitorIndex;
    public void InitProfile(TraitorProfile traitorProfile, int pageIndex)
    {
        for (int i = 0; i < habitTextRenderers.Length; i++)
        {
            Habits behaviour = GetBehaviourAtIndex(traitorProfile.npcProfile.behaviours, i);
            habitTextRenderers[i].SetText(passengersData.habitStringDict[behaviour]);
        }

        int uncoveredMugShotIndex = traitorProfile.mugShotIndex * 2;
        int coveredMugShotIndex = uncoveredMugShotIndex + 1;
        coveredMugShotRenderer.UpdateSpriteInputs(coveredMugShotRenderer.atlas.simpleSprites[coveredMugShotIndex]);

        coveredMugShotRenderer.custom.x = 0;
        coveredMugShotRenderer.custom.y = 0;
        coveredMugShotRenderer.custom.z = 0;
        coveredMugShotRenderer.custom.w = 1;
        coveredMugShotRenderer.customBit &= ~(int)ColorBits.Diagonal;

        uncoveredMugShotRenderer.UpdateSpriteInputs(uncoveredMugShotRenderer.atlas.simpleSprites[uncoveredMugShotIndex]);

        page.Init(pageIndex);
    }

    public void UpdateMugShotReveal(float t)
    {
        coveredMugShotRenderer.custom.x = Mathf.Clamp01(t);
    }
}
