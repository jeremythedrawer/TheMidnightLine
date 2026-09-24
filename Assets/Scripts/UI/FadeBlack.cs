using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;

using static AtlasUI;
public class FadeBlack : MonoBehaviour
{
    public const float DEFAULT_DEPTH = 2;
    public const float NOTEPAD_DEPTH = 14.5f;

    [Flags] public enum State
    { 
        FadingIn,
        FadingOut,
        WritingText,
        FinishedFadeIn,
        FinishedFadeOut,
    }

    public static event Action OnFinishFadeOut;

    public Options options;
    public InputData playerInputs;
    public AudioData audioData;
    public PassengersData passengersData;
    public UIData uidata;

    public Material fadeBlackMaterial;

    public AtlasTextRenderer textRenderer;

    public AudioSource audioSource;

    public TextButton continueButton;

    public CancellationTokenSource ctsFadeBlack;

    [Header("Generated")]
    public State curState;
    public int curSceneIndex;
    public float curUVPosX;
    public float curUVPosY;
    public float curValue;
    private void Start()
    {
        textRenderer.SetText("");
    }
    private void InitButton()
    {
        continueButton.InitButton();
    }
    public void FadeIn(float value, float time, float uvPosX = 0, float uvPosY = 0, float alpha = 0, float fadeBlackZPos = DEFAULT_DEPTH, bool usePassengerStencil = false)
    {
        ctsFadeBlack?.Cancel();
        ctsFadeBlack = new CancellationTokenSource();

        transform.localPosition = new Vector3(0, 0, fadeBlackZPos);
        uidata.fadeBlackWorldPos = transform.position;
        curUVPosX = uvPosX;
        curUVPosY = uvPosY;

        if (usePassengerStencil)
        {
            passengersData.passsengerMaterial.SetFloat("_StencilOp", (float)StencilOp.Replace);
        }
        else
        {
            passengersData.passsengerMaterial.SetFloat("_StencilOp", (float)StencilOp.Keep);
        }
        FadingIn(value, alpha, time).Forget();
    }
    public void FadeOut(float time)
    {
        ctsFadeBlack?.Cancel();
        ctsFadeBlack = new CancellationTokenSource();

        FadingOut(time).Forget();
    }
    public void SetAlpha(float value, float uvPosX = 0, float uvPosY = 0, float alpha = 0)
    {
        fadeBlackMaterial.SetFloat("_Value", value);
        fadeBlackMaterial.SetFloat("_UVPosX", uvPosX);
        fadeBlackMaterial.SetFloat("_UVPosY", uvPosY);
        fadeBlackMaterial.SetFloat("_Alpha", alpha);
    }
    public void WaitAndSetSpacebar(float waitTime)
    {
        WaitingAndSettingSpacebar(waitTime).Forget();
    }
    public void CancelFadeBlack()
    {
        continueButton.gameObject.SetActive(false);
        ctsFadeBlack?.Cancel();
    }

    public void OrTextBit(ColorBits bit)
    {
        textRenderer.customBit |= (int)bit;
    }
    public void AndTextBit(ColorBits bit)
    {
        textRenderer.customBit &= ~(int)bit;
    }
    public void SetText(string text)
    {
        textRenderer.SetText(text);
    }
    public void SetTitleText()
    {
        textRenderer.SetText(options.curTrip.title, alpha: 0);
        audioSource.volume = audioData.soundEffectsVolume;
        audioSource.PlayOneShot(audioData.gong);
    }
    public void WriteTitleText()
    {
        textRenderer.WriteText(options.curTrip.title, options.dayNightTransitionTime / ((float)options.curTrip.title.Length * 4));
    }
    public void AppearText(float time)
    {
        textRenderer.ChangeCustom(time, 1, customChannel: 4);
    }
    public void DissappearText(float time)
    {
        void EmptyText()
        {
            textRenderer.SetText("");
        }
        textRenderer.ChangeCustom(time, 0, customChannel: 4, EmptyText);
    }
    public void SetTitleTextAlpha(float t)
    {
        textRenderer.SetAppearTextAlpha(t);
    }
    private async UniTask WaitingAndSettingSpacebar(float waitTime)
    {
        await UniTask.WaitForSeconds(waitTime, cancellationToken: ctsFadeBlack.Token);
        continueButton.gameObject.SetActive(true);
    }
    private async UniTask FadingIn(float value, float alpha, float time)
    {
        try
        {
            float elapsedTime = fadeBlackMaterial.GetFloat("_Value");
            curState = State.FadingIn;
            float totalTime = time * value;

            fadeBlackMaterial.SetFloat("_UVPosX", curUVPosX);
            fadeBlackMaterial.SetFloat("_UVPosY", curUVPosY);
            fadeBlackMaterial.SetFloat("_Alpha", alpha);
            
            while (elapsedTime < totalTime)
            {
                curValue = (elapsedTime / totalTime) * value;
                fadeBlackMaterial.SetFloat("_Value", curValue);
                elapsedTime += Time.deltaTime;
                await UniTask.Yield(ctsFadeBlack.Token);
            }
            fadeBlackMaterial.SetFloat("_Value", value);
            curState = State.FinishedFadeIn;
        }
        catch (OperationCanceledException) { }
    }
    private async UniTask FadingOut(float time)
    {
        try
        {
            float elapsedTime = curValue * time;
            continueButton.gameObject.SetActive(false);
            curState = State.FadingOut;
            while (elapsedTime > 0)
            {
                curValue = elapsedTime / time;
                fadeBlackMaterial.SetFloat("_Value", curValue);
                textRenderer.SetAppearTextAlpha(curValue);
                elapsedTime -= Time.deltaTime;
                await UniTask.Yield(ctsFadeBlack.Token);
            }
            fadeBlackMaterial.SetFloat("_Value", 0);
            textRenderer.SetAppearTextAlpha(1);
            textRenderer.SetText("");
            curState = State.FinishedFadeOut;
            OnFinishFadeOut?.Invoke();
        }
        catch (OperationCanceledException) { }
    }
}
