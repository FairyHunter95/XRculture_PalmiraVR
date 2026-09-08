using System.Collections;
using UnityEngine;

public class FinalCreditsController : MonoBehaviour
{
    [Header("Activation")]
    public bool playOnStart;

    [Header("Scene Fade")]
    public VRScreenFade vrScreenFade;
    public CanvasGroup fadeOverlayCanvasGroup;
    [Range(0f, 1f)] public float fadeTargetAlpha = 0.75f;
    public float fadeDuration = 5f;

    [Header("Credits")]
    public GameObject creditsRoot;
    public RectTransform creditsContent;
    public float creditsStartY;
    public float creditsEndY;
    public float creditsScrollDuration;

    [Header("Audio")]
    public AudioSource creditsAudioSource;

    [Header("Disable Interactions")]
    public Behaviour[] behavioursToDisable;
    public GameObject[] objectsToDisable;

    [Header("Exit")]
    public float quitDelayAfterCredits = 3f;

    private Coroutine sequenceRoutine;
    private Vector2 initialCreditsPosition;
    private bool initialCreditsPositionCaptured;
    private bool isRunning;

    private void Awake()
    {
        if (fadeOverlayCanvasGroup != null)
        {
            fadeOverlayCanvasGroup.alpha = 0f;
            fadeOverlayCanvasGroup.blocksRaycasts = false;
            fadeOverlayCanvasGroup.interactable = false;
        }

        if (creditsContent != null)
        {
            initialCreditsPosition = creditsContent.anchoredPosition;
            initialCreditsPositionCaptured = true;
            SetCreditsPosition(creditsStartY);
        }

        if (creditsRoot != null)
            creditsRoot.SetActive(false);
    }

    private void Start()
    {
        if (playOnStart)
            BeginCredits();
    }

    private void OnDisable()
    {
        if (sequenceRoutine != null)
            StopCoroutine(sequenceRoutine);

        sequenceRoutine = null;
        isRunning = false;
    }

    public void BeginCredits()
    {
        if (isRunning)
            return;

        if (sequenceRoutine != null)
            StopCoroutine(sequenceRoutine);

        sequenceRoutine = StartCoroutine(PlayCreditsSequence());
    }

    private IEnumerator PlayCreditsSequence()
    {
        isRunning = true;
        DisableInteractions();

        if (vrScreenFade != null)
            yield return StartCoroutine(vrScreenFade.FadeToBlack(Mathf.Max(0.01f, fadeDuration)));
        else if (fadeOverlayCanvasGroup != null)
            yield return StartCoroutine(FadeOverlayRoutine(0f, fadeTargetAlpha, Mathf.Max(0.01f, fadeDuration)));

        if (creditsRoot != null)
            creditsRoot.SetActive(true);

        if (creditsAudioSource != null && creditsAudioSource.clip != null)
        {
            creditsAudioSource.Stop();
            creditsAudioSource.Play();
        }

        if (creditsContent != null)
        {
            SetCreditsPosition(creditsStartY);
            yield return StartCoroutine(ScrollCreditsRoutine());
        }

        if (quitDelayAfterCredits > 0f)
            yield return new WaitForSeconds(quitDelayAfterCredits);

        Application.Quit();
        sequenceRoutine = null;
        isRunning = false;
    }

    private IEnumerator FadeOverlayRoutine(float from, float to, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            fadeOverlayCanvasGroup.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        fadeOverlayCanvasGroup.alpha = to;
    }

    private IEnumerator ScrollCreditsRoutine()
    {
        float duration = Mathf.Max(0.01f, creditsScrollDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetCreditsPosition(Mathf.Lerp(creditsStartY, creditsEndY, t));
            yield return null;
        }

        SetCreditsPosition(creditsEndY);
    }

    private void SetCreditsPosition(float y)
    {
        if (creditsContent == null)
            return;

        Vector2 position = initialCreditsPositionCaptured ? initialCreditsPosition : creditsContent.anchoredPosition;
        position.y = y;
        creditsContent.anchoredPosition = position;
    }

    private void DisableInteractions()
    {
        if (behavioursToDisable != null)
        {
            foreach (Behaviour behaviour in behavioursToDisable)
            {
                if (behaviour != null)
                    behaviour.enabled = false;
            }
        }

        if (objectsToDisable != null)
        {
            foreach (GameObject obj in objectsToDisable)
            {
                if (obj != null)
                    obj.SetActive(false);
            }
        }
    }
}
