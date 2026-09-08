using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class IntroSequenceController : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource audioSource;
    public float delayBeforeAudio = 4f;
    public AudioSource ambienceSource;


    [Header("UI 3D")]
    public GameObject panelRoot;      // UI_IntroPanel

    [Header("Next Level")]
    public string nextSceneName = "4_InsidePalmira";

    [Header("Timing")]
    [Tooltip("Secondi dopo l'inizio dell'audio in cui far comparire il pannello. Se <= 0, appare a fine audio.")]
    public float panelAppearAfterAudioStart;

    [Tooltip("Secondi di attesa dopo la fine dell'audio prima di iniziare il fade.")]
    public float delayAfterAudioEndBeforeFade = 1f;

    [Tooltip("Durata del fade to black.")]
    public float fadeDuration = 4f;

    public VRScreenFade screenFade;

    private void Start()
    {
        if (ambienceSource != null)
        {
            ambienceSource.volume = 0.08f;
            ambienceSource.Play();
        }

        if (panelRoot) panelRoot.SetActive(false);

        StartCoroutine(Sequence());
    }

    private IEnumerator Sequence()
    {
        // Attesa iniziale
        yield return new WaitForSeconds(delayBeforeAudio);

        // Avvia audio
        if (audioSource != null && audioSource.clip != null)
            audioSource.Play();

        // --- Mostra pannello prima della fine audio (configurabile) ---
        if (panelRoot != null && audioSource != null && audioSource.clip != null)
        {
            if (panelAppearAfterAudioStart > 0f)
            {
                float t = Mathf.Min(panelAppearAfterAudioStart, audioSource.clip.length);
                yield return new WaitForSeconds(t);
                panelRoot.SetActive(true);
            }
            else
            {
                // fallback: a fine audio
                yield return new WaitForSeconds(audioSource.clip.length);
                panelRoot.SetActive(true);
            }
        }
        else
        {
            // Se manca audio o pannello, evita crash e continua
            yield return null;
            if (panelRoot) panelRoot.SetActive(true);
        }

        yield return new WaitForSeconds(delayAfterAudioEndBeforeFade);

        if (screenFade != null)
            yield return screenFade.FadeToBlack(fadeDuration);
        else
            yield return new WaitForSeconds(fadeDuration);

        Proceed();
    }

    private void Proceed()
    {
        if (ambienceSource != null)
            ambienceSource.Stop();

        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();

        SceneManager.LoadScene(nextSceneName);
    }
}
