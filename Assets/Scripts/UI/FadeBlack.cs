using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

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
        ReadyToSceneChange,
    }

    public static event Action OnFinishFadeOut;

    public InputData playerInputs;
    public Material fadeBlackMaterial;


    public AtlasTextRenderer textRenderer;
    public TextButton continueButton;

    public CancellationTokenSource ctsFadeBlack;

    [Header("Generated")]
    public State curState;
    public int curSceneIndex;
    public float curUVPosX;
    public float curUVPosY;

    private void Start()
    {
        
    }
    private void InitButton()
    {

        continueButton.InitButton();
    }
    public void FadeInChangeScene(string text, int sceneIndex, float uvPosX = 0, float uvPosY = 0, float alpha = 0, float fadeBlackZPos = DEFAULT_DEPTH)
    {
        ctsFadeBlack?.Cancel();
        ctsFadeBlack = new CancellationTokenSource();
        transform.localPosition = new Vector3(0, 0, fadeBlackZPos);
        curUVPosX = uvPosX;
        curUVPosY = uvPosY;
        FadingInChangeScene(text, sceneIndex, alpha).Forget();
    }
    public void FadeInWithSpacebar(float value, float spacebarWaitTime, float uvPosX = 0, float uvPosY = 0, float alpha = 0, float fadeBlackZPos = DEFAULT_DEPTH)
    {
        ctsFadeBlack?.Cancel();
        ctsFadeBlack = new CancellationTokenSource();

        transform.localPosition = new Vector3(0, 0, fadeBlackZPos);
        curUVPosX = uvPosX;
        curUVPosY = uvPosY;
        FadingInSpacebar(value, alpha, spacebarWaitTime).Forget();
    }
    public void FadeIn(float value, float uvPosX = 0, float uvPosY = 0, float alpha = 0, float fadeBlackZPos = DEFAULT_DEPTH)
    {
        ctsFadeBlack?.Cancel();
        ctsFadeBlack = new CancellationTokenSource();

        transform.localPosition = new Vector3(0, 0, fadeBlackZPos);

        curUVPosX = uvPosX;
        curUVPosY = uvPosY;
        FadingIn(value, alpha).Forget();
    }
    public void CheckToFadeOutSceneChange()
    {
        if (curState == (State.FinishedFadeIn | State.ReadyToSceneChange))
        {
            if (playerInputs.mouseLeftUp || playerInputs.writeKeyDown || playerInputs.move != 0)
            {
                FadeOutChangeScene();
            }
        }
        else if (curState == State.FadingIn || curState == State.WritingText)
        {
            if (playerInputs.writeKeyDown)
            {
                textRenderer.ctsWrite?.Cancel();
            }
        }
    }
    public void FadeOut()
    {
        ctsFadeBlack?.Cancel();
        ctsFadeBlack = new CancellationTokenSource();

        FadingOut().Forget();
    }
    public void FadeOutChangeScene()
    {
        Scenes.SetScene(curSceneIndex);
        ctsFadeBlack?.Cancel();
        ctsFadeBlack = new CancellationTokenSource();

        FadingOut().Forget();
    }
    public void SetAlpha(float value, float uvPosX = 0, float uvPosY = 0, float alpha = 0)
    {
        fadeBlackMaterial.SetFloat("_Value", value);
        fadeBlackMaterial.SetFloat("_UVPosX", uvPosX);
        fadeBlackMaterial.SetFloat("_UVPosY", uvPosY);
        fadeBlackMaterial.SetFloat("_Alpha", alpha);
    }
    private void FinishWritingWithSceneChange()
    {
        curState = (State.FinishedFadeIn | State.ReadyToSceneChange);
        WaitAndSetSpacebar(waitTime: 1);
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
    private async UniTask WaitingAndSettingSpacebar(float waitTime)
    {
        await UniTask.WaitForSeconds(waitTime, cancellationToken: ctsFadeBlack.Token);
        continueButton.gameObject.SetActive(true);
    }
    private async UniTask FadingInChangeScene(string text, int sceneIndex, float alpha = 0)
    {
        try
        {
            float elapsedTime = 0;
            curState = State.FadingIn;

            fadeBlackMaterial.SetFloat("_UVPosX", curUVPosX);
            fadeBlackMaterial.SetFloat("UVPosY", curUVPosY);
            fadeBlackMaterial.SetFloat("_Alpha", alpha);
            
            while (elapsedTime < FADE_BLACK_DURATION)
            {
                float t = elapsedTime / FADE_BLACK_DURATION;
                fadeBlackMaterial.SetFloat("_Value", t);
                elapsedTime += Time.deltaTime;
                await UniTask.Yield(ctsFadeBlack.Token);
            }
            fadeBlackMaterial.SetFloat("_Value", 1);
            curSceneIndex = sceneIndex;
            curState = State.WritingText;
            textRenderer.WriteText(text, PRINT_LETTER_TIME, FinishWritingWithSceneChange, setTextIfCancelled: true);

        }
        catch (OperationCanceledException) { }
    }
    private async UniTask FadingInSpacebar(float newValue, float alpha, float spacebarWaitTime)
    {
        try
        {
            float elapsedTime = fadeBlackMaterial.GetFloat("_Value");

            curState = State.FadingIn;
            float totalTime = FADE_BLACK_DURATION * newValue;
            
            fadeBlackMaterial.SetFloat("_UVPosX", curUVPosX);
            fadeBlackMaterial.SetFloat("_UVPosY", curUVPosY);
            fadeBlackMaterial.SetFloat("_Alpha", alpha);
            
            while (elapsedTime < totalTime)
            {
                float t = (elapsedTime / totalTime) * newValue;
                fadeBlackMaterial.SetFloat("_Value", t);
                elapsedTime += Time.deltaTime;
                await UniTask.Yield(ctsFadeBlack.Token);
            }
            fadeBlackMaterial.SetFloat("_Value", newValue);
            curState = State.FinishedFadeIn;
            WaitAndSetSpacebar(spacebarWaitTime);

        }
        catch (OperationCanceledException) { }
    }
    private async UniTask FadingIn(float value, float alpha)
    {
        try
        {
            float elapsedTime = fadeBlackMaterial.GetFloat("_Value");
            curState = State.FadingIn;
            float totalTime = FADE_BLACK_DURATION * value;

            fadeBlackMaterial.SetFloat("_UVPosX", curUVPosX);
            fadeBlackMaterial.SetFloat("_UVPosY", curUVPosY);
            fadeBlackMaterial.SetFloat("_Alpha", alpha);
            
            while (elapsedTime < totalTime)
            {
                float t = (elapsedTime / totalTime) * value;
                fadeBlackMaterial.SetFloat("_Value", t);
                elapsedTime += Time.deltaTime;
                await UniTask.Yield(ctsFadeBlack.Token);
            }
            fadeBlackMaterial.SetFloat("_Value", value);
            curState = State.FinishedFadeIn;
        }
        catch (OperationCanceledException) { }
    }
    private async UniTask FadingOut()
    {
        try
        {
            float elapsedTime = fadeBlackMaterial.GetFloat("_Value");
            continueButton.gameObject.SetActive(false);
            curState = State.FadingOut;
            while (elapsedTime > 0)
            {
                float t = elapsedTime / FADE_BLACK_DURATION;
                fadeBlackMaterial.SetFloat("_Value", t);
                textRenderer.SetAppearTextAlpha(t);
                elapsedTime -= Time.deltaTime;
                await UniTask.Yield(ctsFadeBlack.Token);
            }
            fadeBlackMaterial.SetFloat("_Value", 0);
            textRenderer.SetAppearTextAlpha(1);
            curState = State.FinishedFadeOut;
            OnFinishFadeOut?.Invoke();
        }
        catch (OperationCanceledException) { }
    }
}
