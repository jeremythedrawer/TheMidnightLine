using System.Threading;
using UnityEngine;
using static AtlasUI;
public class ScoreUI : MonoBehaviour
{
    public TripSO trip;

    public AtlasTextRenderer scoreRenderer;
    public Material fadeBlackMaterial;
    public GameEventDataSO gameEventData;

    public CancellationTokenSource ctsFadeBlack;

    public int traitorsRevealed;
    private void Start()
    {
        FadeFromBlack();
    }
    private void OnEnable()
    {
        gameEventData.OnTraitorsFoundScoreUpdate.RegisterListener(SetTraitorsFoundScore);
    }
    private void OnDisable()
    {
        gameEventData.OnTraitorsFoundScoreUpdate.UnregisterListener(SetTraitorsFoundScore);
    }
    private void FadeFromBlack()
    {
        FadeBlack(fadeBlackMaterial, ctsFadeBlack, toFadeBlack: false);
    }
    public void InitTraitorsFoundScore()
    {
        scoreRenderer.SetText("Traitors found: " + 0 + " / " + trip.traitorProfiles.Length);
    }
    public void SetTraitorsFoundScore()
    {
        traitorsRevealed++;
        scoreRenderer.SetText("Traitors found: " + traitorsRevealed + " / " + trip.traitorProfiles.Length);
    }
}
