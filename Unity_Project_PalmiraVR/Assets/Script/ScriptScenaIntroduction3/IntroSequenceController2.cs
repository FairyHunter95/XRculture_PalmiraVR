using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class IntroSequenceController2 : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource audioSource;        // Narrazione
    public AudioSource ambienceSource;     // Ambiente

    [Header("UI 3D")]
    public GameObject panelRoot;      // UI_IntroPanel

    [Header("Next Level")]
    public string nextSceneName = "4_InsidePalmira";

    [Header("Timings (seconds from scene start)")]
    public float panelAppearDelay;
    public float audioStartDelay;
    public float delayAfterAudioEndBeforeFade = 1f;
    public float fadeDuration = 6f;

    [Header("Ambience")]
    public float ambienceVolume = 0.08f;

    [Header("Fade")]
    public VRScreenFade screenFade;

    private Coroutine flowRoutine;
    private Coroutine panelRoutine;
    private Coroutine audioRoutine;
    private bool audioFinished;
    private bool audioStarted;

    private void Start()
    {
        if (ambienceSource != null)
        {
            ambienceSource.volume = ambienceVolume;
            ambienceSource.loop = true;
            ambienceSource.Play();
        }

        if (panelRoot) panelRoot.SetActive(false);

        panelRoutine = StartCoroutine(ShowPanelAfterDelay());
        audioRoutine = StartCoroutine(PlayAudioAfterDelay());
        flowRoutine = StartCoroutine(Sequence());
    }

    private IEnumerator ShowPanelAfterDelay()
    {
        if (panelAppearDelay > 0f)
            yield return new WaitForSeconds(panelAppearDelay);

        if (panelRoot) panelRoot.SetActive(true);
    }

    private IEnumerator PlayAudioAfterDelay()
    {
        if (audioStartDelay > 0f)
            yield return new WaitForSeconds(audioStartDelay);

        bool hasAudio = audioSource != null && audioSource.clip != null;
        if (hasAudio)
        {
            audioStarted = true;
            audioSource.Stop();
            audioSource.Play();

            while (audioSource.isPlaying)
                yield return null;
        }

        audioFinished = true;
    }

    private IEnumerator Sequence()
    {
        while (!audioFinished)
            yield return null;

        if (delayAfterAudioEndBeforeFade > 0f)
            yield return new WaitForSeconds(delayAfterAudioEndBeforeFade);

        if (screenFade != null)
            yield return screenFade.FadeToBlack(fadeDuration);
        else if (fadeDuration > 0f)
            yield return new WaitForSeconds(fadeDuration);

        Proceed();
    }

    private void Proceed()
    {
        if (flowRoutine != null)
        {
            StopCoroutine(flowRoutine);
            flowRoutine = null;
        }

        if (panelRoutine != null)
        {
            StopCoroutine(panelRoutine);
            panelRoutine = null;
        }

        if (audioRoutine != null)
        {
            StopCoroutine(audioRoutine);
            audioRoutine = null;
        }

        if (ambienceSource != null)
            ambienceSource.Stop();

        if (audioStarted && audioSource != null && audioSource.isPlaying)
            audioSource.Stop();

        SceneManager.LoadScene(nextSceneName);
    }
}
