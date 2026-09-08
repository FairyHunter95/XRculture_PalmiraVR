using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Station4InteractionController : MonoBehaviour
{
    [System.Serializable]
    public class Station4Step
    {
        public string stepName;
        public Button button;
        public TMP_Text label;
        public AudioClip audioClip;
        public int emissionElementIndex;
        public GameObject extraHighlightObject;
        public GameObject[] objectsToHideWhileActive;
        public Graphic completedStateGraphic;
        public Color completedGraphicColor = Color.white;

        [HideInInspector] public bool completed;
        [HideInInspector] public Color initialGraphicColor;
        [HideInInspector] public ColorBlock initialColors;
    }

    [Header("Station")]
    public int stationIndex = 3;
    public AudioSource introNarrationAudio;
    public GameObject interactionPanel;
    public AudioSource stepAudioSource;

    [Header("Final Sequence")]
    public AudioSource finalAudioSource;
    public GameObject finalSlideObject;
    public float finalAudioDelayAfterAllSteps = 4f;
    public float finalSlideDelayFromFinalAudioStart = 5f;
    public string finalExperienceMessage = "Esperienza conclusa grazie per la tua partecipazione.";
    public float finalExperienceMessageDuration = 5f;
    public FinalCreditsController finalCreditsController;

    [Header("Interactions")]
    public EmissionManagerStazione4 emissionManager;
    public Station4Step[] steps;

    private Coroutine stationRoutine;
    private Coroutine stepRoutine;
    private Coroutine finalSequenceRoutine;
    private bool panelActivated;
    private bool finalSequenceStarted;

    private void Awake()
    {
        if (interactionPanel != null)
            interactionPanel.SetActive(false);

        foreach (var step in steps)
        {
            if (step == null)
                continue;

            if (step.completedStateGraphic == null && step.button != null)
                step.completedStateGraphic = step.button.targetGraphic;

            if (step.completedStateGraphic != null)
                step.initialGraphicColor = step.completedStateGraphic.color;

            if (step.button != null)
                step.initialColors = step.button.colors;
        }
    }

    private void OnEnable()
    {
        LessonFlowController.LessonStarted += OnLessonStarted;
    }

    private void OnDisable()
    {
        LessonFlowController.LessonStarted -= OnLessonStarted;

        if (stationRoutine != null)
            StopCoroutine(stationRoutine);

        if (stepRoutine != null)
            StopCoroutine(stepRoutine);

        if (finalSequenceRoutine != null)
            StopCoroutine(finalSequenceRoutine);

        if (stepAudioSource != null)
            stepAudioSource.Stop();

        if (finalAudioSource != null)
            finalAudioSource.Stop();

        if (emissionManager != null)
            emissionManager.ResetAllEmission();
    }

    private void OnLessonStarted(int startedStationIndex)
    {
        if (startedStationIndex != stationIndex)
            return;

        ResetState();

        if (stationRoutine != null)
            StopCoroutine(stationRoutine);

        stationRoutine = StartCoroutine(WaitForIntroThenShowPanel());
    }

    private IEnumerator WaitForIntroThenShowPanel()
    {
        if (introNarrationAudio != null && introNarrationAudio.clip != null)
        {
            while (!introNarrationAudio.isPlaying)
                yield return null;

            while (introNarrationAudio.isPlaying)
                yield return null;
        }

        panelActivated = true;

        if (interactionPanel != null)
            interactionPanel.SetActive(true);

        RefreshButtonStates();
        stationRoutine = null;
    }

    public void PlayStep(int stepIndex)
    {
        if (!panelActivated)
            return;

        if (stepIndex < 0 || stepIndex >= steps.Length)
            return;

        if (stepRoutine != null || steps[stepIndex].completed)
            return;

        stepRoutine = StartCoroutine(PlayStepRoutine(stepIndex));
    }

    private IEnumerator PlayStepRoutine(int stepIndex)
    {
        Station4Step step = steps[stepIndex];
        SetAllButtonsInteractable(false);
        ExperienceAnalyticsLogger.Instance?.LogStation4StepStarted();

        if (emissionManager != null)
            emissionManager.ActivateElementByIndex(step.emissionElementIndex);

        if (step.extraHighlightObject != null)
            step.extraHighlightObject.SetActive(true);

        SetObjectsHiddenWhileActive(step, true);

        if (stepAudioSource != null && step.audioClip != null)
        {
            stepAudioSource.Stop();
            stepAudioSource.clip = step.audioClip;
            stepAudioSource.Play();

            while (stepAudioSource.isPlaying)
                yield return null;
        }

        if (emissionManager != null)
            emissionManager.ResetAllEmission();

        if (step.extraHighlightObject != null)
            step.extraHighlightObject.SetActive(false);

        SetObjectsHiddenWhileActive(step, false);

        step.completed = true;
        ExperienceAnalyticsLogger.Instance?.LogStation4StepCompleted(step.stepName);
        stepRoutine = null;
        ApplyCompletedVisual(step);
        RefreshButtonStates();

        if (AllStepsCompleted() && !finalSequenceStarted)
        {
            finalSequenceStarted = true;

            if (interactionPanel != null)
                interactionPanel.SetActive(false);

            finalSequenceRoutine = StartCoroutine(PlayFinalSequence());
        }
    }

    private void RefreshButtonStates()
    {
        foreach (var step in steps)
        {
            if (step == null || step.button == null)
                continue;

            step.button.interactable = panelActivated;
        }
    }

    private void SetAllButtonsInteractable(bool isInteractable)
    {
        // Keep the buttons visually unchanged; actual locking is enforced in PlayStep.
        if (!isInteractable)
            return;

        RefreshButtonStates();
    }

    private bool AllStepsCompleted()
    {
        if (steps == null || steps.Length == 0)
            return false;

        foreach (var step in steps)
        {
            if (step == null || !step.completed)
                return false;
        }

        return true;
    }

    private void ResetState()
    {
        panelActivated = false;
        finalSequenceStarted = false;

        if (interactionPanel != null)
            interactionPanel.SetActive(false);

        if (finalSlideObject != null)
            finalSlideObject.SetActive(false);

        if (stepAudioSource != null)
            stepAudioSource.Stop();

        if (finalAudioSource != null)
            finalAudioSource.Stop();

        if (emissionManager != null)
            emissionManager.ResetAllEmission();

        foreach (var step in steps)
        {
            if (step == null)
                continue;

            step.completed = false;

            if (step.extraHighlightObject != null)
                step.extraHighlightObject.SetActive(false);

            SetObjectsHiddenWhileActive(step, false);

            if (step.completedStateGraphic != null)
                step.completedStateGraphic.color = step.initialGraphicColor;

            if (step.button != null)
            {
                step.button.colors = step.initialColors;
                step.button.interactable = panelActivated;
            }
        }
    }

    private IEnumerator PlayFinalSequence()
    {
        SetAllButtonsInteractable(false);

        if (finalAudioDelayAfterAllSteps > 0f)
            yield return new WaitForSeconds(finalAudioDelayAfterAllSteps);

        if (finalAudioSource != null && finalAudioSource.clip != null)
        {
            finalAudioSource.Stop();
            finalAudioSource.Play();

            if (finalSlideDelayFromFinalAudioStart >= 0f)
                StartCoroutine(ShowFinalSlideAfterDelay());

            while (finalAudioSource.isPlaying)
                yield return null;
        }
        else
        {
            if (finalSlideDelayFromFinalAudioStart >= 0f)
                yield return StartCoroutine(ShowFinalSlideAfterDelay());
        }

        if (LessonFlowController.Instance != null)
        {
            LessonFlowController.Instance.CompleteLesson(stationIndex);
            LessonFlowController.Instance.ShowFloatingMessage(finalExperienceMessage, finalExperienceMessageDuration);
        }

        ExperienceAnalyticsLogger.Instance?.LogExperienceCompleted();

        if (finalCreditsController != null)
        {
            if (finalExperienceMessageDuration > 0f)
                yield return new WaitForSeconds(finalExperienceMessageDuration);

            finalCreditsController.BeginCredits();
        }

        finalSequenceRoutine = null;
    }

    private IEnumerator ShowFinalSlideAfterDelay()
    {
        if (finalSlideDelayFromFinalAudioStart > 0f)
            yield return new WaitForSeconds(finalSlideDelayFromFinalAudioStart);

        if (finalSlideObject != null)
            finalSlideObject.SetActive(true);
    }

    private void ApplyCompletedVisual(Station4Step step)
    {
        if (step == null || step.completedStateGraphic == null)
            return;

        step.completedStateGraphic.color = step.completedGraphicColor;

        if (step.button != null)
        {
            ColorBlock colors = step.button.colors;
            colors.normalColor = step.completedGraphicColor;
            colors.highlightedColor = step.completedGraphicColor;
            colors.pressedColor = step.completedGraphicColor;
            colors.selectedColor = step.completedGraphicColor;
            step.button.colors = colors;
        }
    }

    private void SetObjectsHiddenWhileActive(Station4Step step, bool isActive)
    {
        if (step == null || step.objectsToHideWhileActive == null)
            return;

        foreach (GameObject target in step.objectsToHideWhileActive)
        {
            if (target == null)
                continue;

            target.SetActive(!isActive);
        }
    }

    public void PlayStep0() => PlayStep(0);
    public void PlayStep1() => PlayStep(1);
    public void PlayStep2() => PlayStep(2);
    public void PlayStep3() => PlayStep(3);
    public void PlayStep4() => PlayStep(4);
}
