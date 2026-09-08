using System;
using System.Collections;
using UnityEngine;
using TMPro;

public class LessonFlowController : MonoBehaviour
{
    public static LessonFlowController Instance { get; private set; }
    public static event Action<int> LessonStarted;
    public static event Action<int> LessonCompleted;

    [Header("Stations")]
    public LessonStation[] stations;

    [Header("Player")]
    public Transform xrOrigin;

    [Header("Locomotion Lock At Lesson Start")]
    [Tooltip("Per quanti secondi bloccare movimento e rotazione con joystick all'inizio di ogni lezione.")]
    public float lockMovementAtLessonStartSeconds;

    [Tooltip("Componenti di locomozione da disabilitare temporaneamente (es. move provider, turn provider).")]
    public MonoBehaviour[] locomotionProvidersToDisable;

    [Header("Start (free roam)")]
    public bool startFreeRoam = true;
    public float forceStartAfterSeconds = 20f;

    [Header("Reach next station (teleport)")]
    public float timeToReachNextStation = 20f;
    public float warningBeforeTeleport = 5f;

    [Header("UI - Floating Message (Text + Background)")]
    [Tooltip("Parent che contiene sia il testo TMP che il quad di sfondo.")]
    public GameObject floatingMessageRoot;

    [Tooltip("Il TextMeshPro dentro floatingMessageRoot.")]
    public TextMeshPro floatingText;

    [Tooltip("Quad o oggetto di sfondo del messaggio da ridimensionare automaticamente.")]
    public Transform floatingMessageBackground;

    [Tooltip("Margine aggiuntivo sullo sfondo del messaggio.")]
    public Vector2 floatingMessageBackgroundPadding = new Vector2(0.15f, 0.08f);

    public float messageDuration = 2.0f;

    [Header("Lesson UI Timing")]
    [Tooltip("Dopo quanti secondi dal messaggio 'Lesson completed' mostra 'Reach station ...'.")]
    public float delayAfterLessonEndedMessage = 2f;

    [Header("Lesson Audio Timing")]
    [Tooltip("Secondi di attesa dopo l'ingresso nella stazione prima di far partire l'audio.")]
    public float delayBeforeAudio;

    [Tooltip("Secondi di attesa dopo la fine dell'audio prima di considerare conclusa la lezione.")]
    public float delayAfterAudio;

    [Header("Debug")]
    public int debugStartStation = -1;
    public bool debugTeleportOnStart = true;

    private int currentIndex = -1;
    private int expectedNextIndex = -1;

    private Coroutine lessonCoroutine;
    private Coroutine eventsCoroutine;
    private Coroutine reachCoroutine;
    private Coroutine forceStartCoroutine;

    private Coroutine messageCoroutine;
    private Coroutine countdownCoroutine;
    private Coroutine reachMessageCoroutine;
    private Coroutine locomotionLockCoroutine;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (stations == null || stations.Length == 0)
        {
            Debug.LogError("LessonFlowController: stations non assegnate!");
            return;
        }

        HideFloatingMessageInstant();

        if (debugStartStation >= 0 && debugStartStation < stations.Length)
        {
            currentIndex = debugStartStation;

            ActivateStation(currentIndex);

            if (debugTeleportOnStart)
                TeleportToStation(currentIndex);

            StartLesson(currentIndex);
            return;
        }

        if (startFreeRoam)
        {
            currentIndex = -1;
            expectedNextIndex = -1;

            for (int i = 0; i < stations.Length; i++)
                stations[i].SetActiveStation(false);

            ShowMessage("Muoviti alla prima lezione", 3f);

            if (forceStartAfterSeconds > 0f)
                forceStartCoroutine = StartCoroutine(ForceStartRoutine());
        }
        else
        {
            currentIndex = 0;
            ActivateStation(currentIndex);
            StartLesson(currentIndex);
        }
    }

    private IEnumerator ForceStartRoutine()
    {
        yield return new WaitForSeconds(forceStartAfterSeconds);

        if (currentIndex == -1)
        {
            Debug.Log("ForceStart: teleport to Station 01 and start lesson.");
            ShowMessage("Raggiungi la prima lezione...", 2.5f);
            ForceStartStation01();
        }
    }

    private void ForceStartStation01()
    {
        StopForceStart();

        currentIndex = 0;
        expectedNextIndex = -1;

        TeleportToStation(0);
        ActivateStation(0);
        StartLesson(0);
    }

    private void StopForceStart()
    {
        if (forceStartCoroutine != null)
        {
            StopCoroutine(forceStartCoroutine);
            forceStartCoroutine = null;
        }
    }

    public void OnStationEntered(LessonStation station)
    {
        if (station == null) return;

        Debug.Log($"Entrato in Station_{station.stationIndex + 1:00}");
        ExperienceAnalyticsLogger.Instance?.LogStationTriggerEntered(station.stationIndex);

        if (currentIndex == -1 && station.stationIndex == 0)
        {
            StopForceStart();
            currentIndex = 0;

            ActivateStation(0);
            StartLesson(0);
            return;
        }

        if (expectedNextIndex >= 0 && station.stationIndex == expectedNextIndex)
        {
            StopReachTimer();
            StopCountdown();

            GoToStation(expectedNextIndex);
        }
    }

    private void GoToStation(int index)
    {
        currentIndex = index;
        expectedNextIndex = -1;

        ActivateStation(currentIndex);
        StartLesson(currentIndex);
    }

    private void ActivateStation(int index)
    {
        Debug.Log($"Attivo Station_{index + 1:00}");

        for (int i = 0; i < stations.Length; i++)
            stations[i].SetActiveStation(i == index);
    }

    private void StartLesson(int index)
    {
        StopLessonTimer();
        StopEvents();
        StopCountdown();
        StopLocomotionLock();

        if (reachMessageCoroutine != null)
        {
            StopCoroutine(reachMessageCoroutine);
            reachMessageCoroutine = null;
        }

        LessonStation st = stations[index];

        if (st.markerRoot != null)
            st.markerRoot.SetActive(false);

        if (delayBeforeAudio > 0f)
            StartCountdown($"La lezione {index + 1} inizia tra", Mathf.CeilToInt(delayBeforeAudio));
        else
            ShowMessage($"La lezione {index + 1} inizia", messageDuration);

        StartStationEvents(st);
        StartLocomotionLock();
        LessonStarted?.Invoke(index);
        ExperienceAnalyticsLogger.Instance?.LogLessonStarted(index);

        lessonCoroutine = StartCoroutine(LessonRoutine(st));
    }

    private IEnumerator LessonRoutine(LessonStation st)
    {
        Debug.Log($"Lezione Station_{st.stationIndex + 1:00} - mode: {st.endMode}");

        if (st.endMode == LessonEndMode.Timer)
        {
            yield return new WaitForSeconds(st.lessonDuration);
            CompleteLesson(st.stationIndex);
        }
        else if (st.endMode == LessonEndMode.AudioEnd)
        {
            if (delayBeforeAudio > 0f)
                yield return new WaitForSeconds(delayBeforeAudio);

            if (st.narrationAudio == null || st.narrationAudio.clip == null)
            {
                yield return new WaitForSeconds(st.lessonDuration);
                CompleteLesson(st.stationIndex);
            }
            else
            {
                st.narrationAudio.Stop();
                st.narrationAudio.Play();

                while (st.narrationAudio.isPlaying)
                    yield return null;

                if (delayAfterAudio > 0f)
                    yield return new WaitForSeconds(delayAfterAudio);

                CompleteLesson(st.stationIndex);
            }
        }
        else
        {
            if (delayBeforeAudio > 0f)
                yield return new WaitForSeconds(delayBeforeAudio);

            if (st.narrationAudio != null && st.narrationAudio.clip != null)
            {
                st.narrationAudio.Stop();
                st.narrationAudio.Play();
            }

            yield break;
        }
    }

    public void CompleteLesson(int stationIndex)
    {
        if (stationIndex != currentIndex) return;

        Debug.Log($"Lezione completata Station_{stationIndex + 1:00}");
        LessonCompleted?.Invoke(stationIndex);
        ExperienceAnalyticsLogger.Instance?.LogLessonCompleted(stationIndex);

        if (stationIndex >= 0 && stationIndex < stations.Length)
            stations[stationIndex].SetActiveStation(false);

        ShowMessage($"Lezione {stationIndex + 1} completata", messageDuration);

        if (currentIndex >= stations.Length - 1)
        {
            return;
        }

        expectedNextIndex = currentIndex + 1;

        if (reachMessageCoroutine != null)
            StopCoroutine(reachMessageCoroutine);

        reachMessageCoroutine = StartCoroutine(ShowReachMessageAfterDelay(expectedNextIndex, delayAfterLessonEndedMessage));

        StopReachTimer();
        reachCoroutine = StartCoroutine(ReachNextRoutine(expectedNextIndex));
    }

    public void TeleportAndStartStation(int index)
    {
        if (stations == null || index < 0 || index >= stations.Length)
            return;

        StopReachTimer();
        StopCountdown();
        StopForceStart();

        TeleportToStation(index);
        GoToStation(index);
    }

    public void TeleportAndPrepareStation(int index)
    {
        if (stations == null || index < 0 || index >= stations.Length)
            return;

        StopReachTimer();
        StopCountdown();
        StopForceStart();
        StopLessonTimer();
        StopEvents();
        StopLocomotionLock();

        if (reachMessageCoroutine != null)
        {
            StopCoroutine(reachMessageCoroutine);
            reachMessageCoroutine = null;
        }

        currentIndex = index;
        expectedNextIndex = -1;

        TeleportToStation(index);
        ActivateStation(index);
    }

    public void StartPreparedStation(int index)
    {
        if (stations == null || index < 0 || index >= stations.Length)
            return;

        if (currentIndex != index)
        {
            currentIndex = index;
            expectedNextIndex = -1;
            ActivateStation(index);
        }

        StartLesson(index);
    }

    private IEnumerator ShowReachMessageAfterDelay(int nextIndex, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        ShowMessage($"Raggiungi la lezione {nextIndex + 1}", 3f);
        reachMessageCoroutine = null;
    }

    private IEnumerator ReachNextRoutine(int nextIndex)
    {
        float t = 0f;
        bool warned = false;
        float warnAt = Mathf.Max(0f, timeToReachNextStation - warningBeforeTeleport);

        while (t < timeToReachNextStation)
        {
            t += Time.deltaTime;

            if (!warned && warningBeforeTeleport > 0f && t >= warnAt)
            {
                warned = true;
                int seconds = Mathf.CeilToInt(warningBeforeTeleport);
                StartCountdown("Teleporting in", seconds);
            }

            yield return null;
        }

        ShowMessage("Teletrasporto...", 1.5f);
        ExperienceAnalyticsLogger.Instance?.LogTeleportBetweenStations(currentIndex, nextIndex);

        TeleportToStation(nextIndex);
        GoToStation(nextIndex);
    }

    private void TeleportToStation(int index)
    {
        if (xrOrigin == null) return;
        if (stations[index].spawnPoint == null) return;

        xrOrigin.SetPositionAndRotation(
            stations[index].spawnPoint.position,
            stations[index].spawnPoint.rotation
        );
    }

    private void StartStationEvents(LessonStation st)
    {
        if (st.events == null || st.events.Length == 0)
            return;

        eventsCoroutine = StartCoroutine(RunStationEvents(st));
    }

    private IEnumerator RunStationEvents(LessonStation st)
    {
        StationTimedEvent[] list = (StationTimedEvent[])st.events.Clone();
        Array.Sort(list, (a, b) => a.time.CompareTo(b.time));

        float startTime = Time.time;

        foreach (var e in list)
        {
            if (e == null) continue;

            float wait = (startTime + e.time) - Time.time;
            if (wait > 0f)
                yield return new WaitForSeconds(wait);

            if (e.target != null)
            {
                e.target.SetActive(e.setActive);

                if (e.revertAfter && e.duration > 0f)
                {
                    yield return new WaitForSeconds(e.duration);
                    if (e.target != null)
                        e.target.SetActive(!e.setActive);
                }
            }
        }
    }

    private void ShowMessage(string msg, float duration)
    {
        if (floatingMessageRoot == null || floatingText == null) return;

        StopCountdown();

        if (messageCoroutine != null)
            StopCoroutine(messageCoroutine);

        floatingText.text = msg;
        ResizeFloatingMessageBackground();
        floatingMessageRoot.SetActive(true);

        messageCoroutine = StartCoroutine(HideMessageAfter(duration));
    }

    public void ShowMapMessage(string msg, float duration)
    {
        ShowMessage(msg, duration);
    }

    public void ShowFloatingMessage(string msg, float duration)
    {
        ShowMessage(msg, duration);
    }

    private IEnumerator HideMessageAfter(float duration)
    {
        yield return new WaitForSeconds(duration);

        HideFloatingMessageInstant();
        messageCoroutine = null;
    }

    private void HideFloatingMessageInstant()
    {
        if (floatingMessageRoot != null)
            floatingMessageRoot.SetActive(false);
    }

    private void ResizeFloatingMessageBackground()
    {
        if (floatingText == null || floatingMessageBackground == null)
            return;

        floatingText.ForceMeshUpdate();
        Vector2 preferredSize = floatingText.GetPreferredValues(floatingText.text);
        Vector3 scale = floatingMessageBackground.localScale;

        scale.x = preferredSize.x + floatingMessageBackgroundPadding.x;
        scale.y = preferredSize.y + floatingMessageBackgroundPadding.y;

        floatingMessageBackground.localScale = scale;
    }

    private void StartCountdown(string prefix, int seconds)
    {
        if (floatingMessageRoot == null || floatingText == null) return;

        if (countdownCoroutine != null)
            StopCoroutine(countdownCoroutine);

        countdownCoroutine = StartCoroutine(CountdownRoutine(prefix, seconds));
    }

    private IEnumerator CountdownRoutine(string prefix, int seconds)
    {
        floatingMessageRoot.SetActive(true);

        for (int t = seconds; t > 0; t--)
        {
            floatingText.text = $"{prefix} {t}...";
            yield return new WaitForSeconds(1f);
        }

        HideFloatingMessageInstant();
        countdownCoroutine = null;
    }

    private void StopCountdown()
    {
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }
    }

    private void StartLocomotionLock()
    {
        if (lockMovementAtLessonStartSeconds <= 0f)
            return;

        SetLocomotionProvidersEnabled(false);
        locomotionLockCoroutine = StartCoroutine(UnlockLocomotionAfterDelay());
    }

    private IEnumerator UnlockLocomotionAfterDelay()
    {
        yield return new WaitForSeconds(lockMovementAtLessonStartSeconds);

        SetLocomotionProvidersEnabled(true);
        locomotionLockCoroutine = null;
    }

    private void StopLocomotionLock()
    {
        if (locomotionLockCoroutine != null)
        {
            StopCoroutine(locomotionLockCoroutine);
            locomotionLockCoroutine = null;
        }

        SetLocomotionProvidersEnabled(true);
    }

    private void SetLocomotionProvidersEnabled(bool isEnabled)
    {
        if (locomotionProvidersToDisable == null)
            return;

        foreach (var provider in locomotionProvidersToDisable)
        {
            if (provider != null)
                provider.enabled = isEnabled;
        }
    }

    private void StopLessonTimer()
    {
        if (lessonCoroutine != null)
        {
            StopCoroutine(lessonCoroutine);
            lessonCoroutine = null;
        }
    }

    private void StopEvents()
    {
        if (eventsCoroutine != null)
        {
            StopCoroutine(eventsCoroutine);
            eventsCoroutine = null;
        }
    }

    private void StopReachTimer()
    {
        if (reachCoroutine != null)
        {
            StopCoroutine(reachCoroutine);
            reachCoroutine = null;
        }
    }

    private void OnDisable()
    {
        StopLocomotionLock();
    }
}
