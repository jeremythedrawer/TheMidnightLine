using UnityEngine;

using static AtlasUI;
public class CamUIController : MonoBehaviour
{
    public FadeBlack fadeBlack;

    public CameraData camData;
    public void Start()
    {
        Init();
    }
    private void Init()
    {
        fadeBlack.SetAlpha(0);
        fadeBlack.FadeOut();
    }
    public void WriteTitleText()
    {
        fadeBlack.AndTextBit(ColorBits.Invert);
        fadeBlack.WriteTitleText();
    }
    public void SetTitleAlpha(float alpha)
    {
        fadeBlack.SetTitleTextAlpha(alpha);
    }
    public void DissappearTitleAlpha()
    {
        fadeBlack.DissappearText(1f);
    }
    public void SetTitleText()
    {
        fadeBlack.SetTitleText();
    }
}
