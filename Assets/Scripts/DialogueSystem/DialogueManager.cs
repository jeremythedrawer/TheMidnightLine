using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    [SerializeField] GameEventDataSO gameEventData;
    [SerializeField] GameFadeSO gameFadeSO;
    [SerializeField] TutorialManager tutorialManager;

    private void OnEnable()
    {
        gameEventData.OnStartTutorial.RegisterListener(StartTutorial);
    }

    private void OnDisable()
    {
        gameEventData.OnStartTutorial.UnregisterListener(StartTutorial);
    }

    private void StartTutorial()
    {
        gameEventData.OnGameFadeIn.Raise();
        WaitForFade().Forget();
    }

    private async UniTask WaitForFade()
    {
        while (gameFadeSO.brightness != 1) await UniTask.Yield();
        Instantiate(tutorialManager, transform);
    }



}
