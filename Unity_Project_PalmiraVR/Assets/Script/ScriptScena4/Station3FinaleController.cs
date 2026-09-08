using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class Station3FinaleController : MonoBehaviour
{
    [Header("Station")]
    public int stationIndex = 2;
    public int nextStationIndex = 3;
    public AudioSource narrationAudioSource;
    public float ticketDelayAfterNarrationEnds = 3f;

    [Header("Audience")]
    public GameObject pubblicoRoot;
    public float pubblicoAppearDelay = 8f;
    public float pubblicoVisibleDuration = 17f;
    public AudioSource pubblicoAudioSource;

    [Header("Ticket")]
    public GameObject ticketObject;
    public XRGrabInteractable ticketGrabInteractable;
    public bool showTicketImmediately;

    [Header("Transition")]
    public VRScreenFade vrScreenFade;
    public CanvasGroup transitionFadeCanvasGroup;
    public float fadeOutDuration;
    public float fadeInDuration;
    public AudioSource transitionAudioSource;

    [Header("Final Cleanup")]
    public GameObject capitelloObject;

    private Coroutine stationRoutine;
    private Coroutine transitionRoutine;
    private bool ticketConsumed;

    private void Awake()
    {
        if (pubblicoRoot != null)
            pubblicoRoot.SetActive(false);

        if (ticketObject != null)
            ticketObject.SetActive(false);

        if (transitionFadeCanvasGroup != null)
        {
            transitionFadeCanvasGroup.alpha = 0f;
            transitionFadeCanvasGroup.blocksRaycasts = false;
            transitionFadeCanvasGroup.interactable = false;
        }
    }

    private void OnEnable()
    {
        LessonFlowController.LessonStarted += OnLessonStarted;

        if (ticketGrabInteractable != null)
            ticketGrabInteractable.selectEntered.AddListener(OnTicketGrabbed);
    }

    private void OnDisable()
    {
        LessonFlowController.LessonStarted -= OnLessonStarted;

        if (ticketGrabInteractable != null)
            ticketGrabInteractable.selectEntered.RemoveListener(OnTicketGrabbed);

        if (transitionRoutine != null)
            StopCoroutine(transitionRoutine);
    }

    private void OnLessonStarted(int startedStationIndex)
    {
        if (startedStationIndex != stationIndex)
            return;

        ResetState();
        ExperienceAnalyticsLogger.Instance?.StartWatchingCapitello(capitelloObject != null ? capitelloObject.transform : null);

        if (stationRoutine != null)
            StopCoroutine(stationRoutine);

        stationRoutine = StartCoroutine(StationRoutine());
    }

    private IEnumerator StationRoutine()
    {
        if (pubblicoAppearDelay > 0f)
            yield return new WaitForSeconds(pubblicoAppearDelay);

        if (pubblicoRoot != null)
        {
            pubblicoRoot.SetActive(true);
            ExperienceAnalyticsLogger.Instance?.StartWatchingPubblico(pubblicoRoot.transform);
        }

        if (pubblicoAudioSource != null && pubblicoAudioSource.clip != null)
        {
            pubblicoAudioSource.Stop();
            pubblicoAudioSource.Play();
        }

        if (pubblicoVisibleDuration > 0f)
            StartCoroutine(HidePubblicoAfterDelay());

        if (narrationAudioSource != null && narrationAudioSource.clip != null)
        {
            if (!showTicketImmediately)
            {
                while (!narrationAudioSource.isPlaying)
                    yield return null;

                while (narrationAudioSource.isPlaying)
                    yield return null;
            }
        }

        if (!showTicketImmediately && ticketDelayAfterNarrationEnds > 0f)
            yield return new WaitForSeconds(ticketDelayAfterNarrationEnds);

        if (ticketObject != null)
        {
            ticketObject.SetActive(true);
            ExperienceAnalyticsLogger.Instance?.LogTicketShown();
        }

        stationRoutine = null;
    }

    private void OnTicketGrabbed(SelectEnterEventArgs args)
    {
        if (ticketConsumed)
            return;

        ticketConsumed = true;
        ExperienceAnalyticsLogger.Instance?.LogTicketGrabbed();

        if (transitionRoutine != null)
            StopCoroutine(transitionRoutine);

        transitionRoutine = StartCoroutine(TransitionToNextStationRoutine());
    }

    private void ResetState()
    {
        ticketConsumed = false;

        if (pubblicoAudioSource != null)
            pubblicoAudioSource.Stop();

        if (transitionAudioSource != null)
            transitionAudioSource.Stop();

        if (pubblicoRoot != null)
            pubblicoRoot.SetActive(false);

        ExperienceAnalyticsLogger.Instance?.StopWatchingPubblico();
        ExperienceAnalyticsLogger.Instance?.StopWatchingCapitello();

        if (ticketObject != null)
            ticketObject.SetActive(false);
    }

    private IEnumerator HidePubblicoAfterDelay()
    {
        yield return new WaitForSeconds(pubblicoVisibleDuration);

        if (pubblicoAudioSource != null && pubblicoAudioSource.isPlaying)
            pubblicoAudioSource.Stop();

        if (pubblicoRoot != null)
            pubblicoRoot.SetActive(false);
    }

    private IEnumerator TransitionToNextStationRoutine()
    {
        if (ticketObject != null)
            ticketObject.SetActive(false);

        if (transitionAudioSource != null && transitionAudioSource.clip != null)
        {
            transitionAudioSource.Stop();
            transitionAudioSource.Play();
        }

        if (vrScreenFade != null)
            yield return StartCoroutine(vrScreenFade.FadeToBlack(fadeOutDuration));
        else
            yield return StartCoroutine(FadeRoutine(0f, 1f, fadeOutDuration));

        if (pubblicoAudioSource != null && pubblicoAudioSource.isPlaying)
            pubblicoAudioSource.Stop();

        if (pubblicoRoot != null)
            pubblicoRoot.SetActive(false);

        ExperienceAnalyticsLogger.Instance?.StopWatchingPubblico();

        if (capitelloObject != null)
            capitelloObject.SetActive(false);

        ExperienceAnalyticsLogger.Instance?.StopWatchingCapitello();

        if (LessonFlowController.Instance != null)
            LessonFlowController.Instance.TeleportAndPrepareStation(nextStationIndex);

        ExperienceAnalyticsLogger.Instance?.LogTeleportBetweenStations(stationIndex, nextStationIndex);

        if (transitionAudioSource != null && transitionAudioSource.isPlaying)
            transitionAudioSource.Stop();

        if (vrScreenFade != null)
            yield return StartCoroutine(vrScreenFade.FadeFromBlack(fadeInDuration));
        else
            yield return StartCoroutine(FadeRoutine(1f, 0f, fadeInDuration));

        if (LessonFlowController.Instance != null)
            LessonFlowController.Instance.StartPreparedStation(nextStationIndex);

        transitionRoutine = null;
    }

    private IEnumerator FadeRoutine(float from, float to, float duration)
    {
        if (transitionFadeCanvasGroup == null)
            yield break;

        transitionFadeCanvasGroup.blocksRaycasts = true;
        transitionFadeCanvasGroup.interactable = true;
        transitionFadeCanvasGroup.alpha = from;

        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.01f, duration);

        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);
            transitionFadeCanvasGroup.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        transitionFadeCanvasGroup.alpha = to;

        bool overlayVisible = to > 0.001f;
        transitionFadeCanvasGroup.blocksRaycasts = overlayVisible;
        transitionFadeCanvasGroup.interactable = overlayVisible;
    }
}
