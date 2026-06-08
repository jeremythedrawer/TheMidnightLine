using System.Threading;
using UnityEngine;
using static AtlasUI;
public class ScoreUI : MonoBehaviour
{
    public Material fadeBlackMaterial;
    public CancellationTokenSource ctsFadeBlack;

    private void OnEnable()
    {
        FadeFromBlack();
    }
    private void FadeFromBlack()
    {
        FadeBlack(fadeBlackMaterial, ctsFadeBlack, toFadeBlack: false);
    }
}
