using System.Collections;
using UnityEngine;

public class MappaController : MonoBehaviour
{
    [Header("Station")]
    [Tooltip("Indice della stazione da completare quando l'utente trova Palmira.")]
    public int stationIndex = 0;

    public float messageDuration = 3f;

    [Header("Correct Answer")]
    public GameObject testoPalmira;
    public GameObject markerPalmiraCorrect;
    public string correctMessage = "Complimenti hai trovato Palmira";
    public float completeLessonDelay = 4f;

    [Header("Audio Feedback")]
    public AudioSource feedbackAudioSource;
    public AudioClip correctAnswerClip;
    public AudioClip wrongAnswerClip;

    [Header("Wrong Answers")]
    public GameObject testoAlessandria;
    public GameObject testoRoma;
    public GameObject testoAtene;
    public GameObject markerPalmiraAlessandria;
    public GameObject markerPalmiraRoma;
    public GameObject markerPalmiraAtene;
    public GameObject bottoneAlessandria;
    public GameObject bottoneRoma;
    public GameObject bottoneAtene;
    public string wrongMessageAlessandria = "Hai sbagliato, hai selezionato Alessandria";
    public string wrongMessageRoma = "Hai sbagliato, hai selezionato Roma";
    public string wrongMessageAtene = "Hai sbagliato, hai selezionato Atene";
    private bool answerFound;

    private void Awake()
    {
        if (testoPalmira != null)
            testoPalmira.SetActive(false);

        if (testoAlessandria != null)
            testoAlessandria.SetActive(false);

        if (testoRoma != null)
            testoRoma.SetActive(false);

        if (testoAtene != null)
            testoAtene.SetActive(false);
    }

    private void OnEnable()
    {
        ExperienceAnalyticsLogger.Instance?.LogMapShown();
    }

    public void SelectPalmira()
    {
        if (answerFound)
            return;

        answerFound = true;
        ExperienceAnalyticsLogger.Instance?.LogMapAnswer("Palmira", true);

        if (markerPalmiraCorrect != null)
            markerPalmiraCorrect.SetActive(false);

        if (testoPalmira != null)
            testoPalmira.SetActive(true);

        PlayFeedback(correctAnswerClip);
        ShowMessage(correctMessage);
        StartCoroutine(CompleteLessonAfterDelay());
    }

    public void SelectAlessandria()
    {
        if (answerFound)
            return;

        ExperienceAnalyticsLogger.Instance?.LogMapAnswer("Alessandria", false);
        PlayFeedback(wrongAnswerClip);
        ConsumeWrongAnswer(testoAlessandria, markerPalmiraAlessandria, bottoneAlessandria);
        ShowMessage(wrongMessageAlessandria);
    }

    public void SelectRoma()
    {
        if (answerFound)
            return;

        ExperienceAnalyticsLogger.Instance?.LogMapAnswer("Roma", false);
        PlayFeedback(wrongAnswerClip);
        ConsumeWrongAnswer(testoRoma, markerPalmiraRoma, bottoneRoma);
        ShowMessage(wrongMessageRoma);
    }

    public void SelectAtene()
    {
        if (answerFound)
            return;

        ExperienceAnalyticsLogger.Instance?.LogMapAnswer("Atene", false);
        PlayFeedback(wrongAnswerClip);
        ConsumeWrongAnswer(testoAtene, markerPalmiraAtene, bottoneAtene);
        ShowMessage(wrongMessageAtene);
    }

    private void ShowMessage(string message)
    {
        if (LessonFlowController.Instance == null)
            return;

        LessonFlowController.Instance.ShowMapMessage(message, messageDuration);
    }

    private void PlayFeedback(AudioClip clip)
    {
        if (feedbackAudioSource == null || clip == null)
            return;

        feedbackAudioSource.PlayOneShot(clip);
    }

    private void ConsumeWrongAnswer(GameObject testo, GameObject marker, GameObject bottone)
    {
        if (testo != null)
            testo.SetActive(true);

        if (marker != null)
            marker.SetActive(false);

        if (bottone != null)
            bottone.SetActive(false);
    }

    private IEnumerator CompleteLessonAfterDelay()
    {
        yield return new WaitForSeconds(completeLessonDelay);

        if (LessonFlowController.Instance != null)
            LessonFlowController.Instance.CompleteLesson(stationIndex);
    }
}
