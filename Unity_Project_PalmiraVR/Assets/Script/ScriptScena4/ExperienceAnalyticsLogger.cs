using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

public class ExperienceAnalyticsLogger : MonoBehaviour
{
    public static ExperienceAnalyticsLogger Instance { get; private set; }

    [Header("Session")]
    public bool dontDestroyOnLoad = true;
    public string analyticsFolderName = "Analytics";
    public bool mirrorCsvToProjectFolderOnDesktop = true;
    public string desktopMirrorFolderName = "AnalyticsExports";

    [Header("Tracking")]
    public float triggerDebounceSeconds = 1f;
    public float lookSampleInterval = 0.2f;
    [Range(0f, 1f)] public float lookDotThreshold = 0.75f;

    private readonly Dictionary<int, float> lastStationTriggerTimes = new Dictionary<int, float>();
    private readonly Dictionary<int, float> lessonStartTimes = new Dictionary<int, float>();
    private readonly Dictionary<int, float> lessonCompleteTimes = new Dictionary<int, float>();
    private readonly Dictionary<int, float> transitionStartTimes = new Dictionary<int, float>();
    private readonly Dictionary<int, bool> transitionTeleported = new Dictionary<int, bool>();
    private readonly Dictionary<string, bool> station2UniqueSelections = new Dictionary<string, bool>();
    private readonly HashSet<string> station4CompletedSteps = new HashSet<string>();

    private string sessionId;
    private string summaryFilePath;
    private string desktopSummaryFilePath;
    private const string CsvSeparator = ";";

    private float sessionStartTime;
    private DateTime sessionStartDateTime;
    private bool sessionCompleted;
    private bool summaryWritten;

    private float mapShownTime = -1f;
    private int mapErrorsBeforeSuccess;
    private bool mapSolved;
    private float mapDuration = -1f;

    private int station2SelectionClicks;

    private bool pubblicoLooked;
    private bool capitelloLooked;
    private bool capitelloMaterialChanged;

    private float ticketShownTime = -1f;

    private float station4TaskStartTime = -1f;
    private float station4TaskDuration = -1f;

    private float st1ToSt2 = -1f;
    private float st2ToSt3 = -1f;
    private float st3ToSt4 = -1f;

    private int totalTeleports;

    private bool pubblicoWatchActive;
    private Transform pubblicoWatchTarget;
    private bool capitelloWatchActive;
    private Transform capitelloWatchTarget;
    private float nextLookSampleTime;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);

        sessionId = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        sessionStartDateTime = DateTime.Now;
        sessionStartTime = Time.realtimeSinceStartup;

        string analyticsDirectory = Path.Combine(Application.persistentDataPath, analyticsFolderName);
        Directory.CreateDirectory(analyticsDirectory);
        string summaryFileName = $"session_summary_{sessionStartDateTime.ToString("ddMMyy_HHmm", CultureInfo.InvariantCulture)}.csv";
        summaryFilePath = Path.Combine(analyticsDirectory, summaryFileName);

        if (mirrorCsvToProjectFolderOnDesktop && (Application.isEditor || Application.platform == RuntimePlatform.WindowsPlayer))
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            if (!string.IsNullOrEmpty(projectRoot))
            {
                string desktopDirectory = Path.Combine(projectRoot, desktopMirrorFolderName);
                Directory.CreateDirectory(desktopDirectory);
                desktopSummaryFilePath = Path.Combine(desktopDirectory, summaryFileName);
            }
        }

        string header = string.Join(CsvSeparator, new[]
        {
            "session_id",
            "start_time",
            "end_time",
            "durata_totale",
            "precisione_mappa",
            "durata_mappa",
            "selezione_architettura",
            "riselezione",
            "pubblico",
            "capitello",
            "capitello_materiale",
            "pannello_parti_teatro",
            "st1_st2",
            "st2_st3",
            "st3_st4",
            "teletrasporti_totali"
        }) + Environment.NewLine;

        EnsureFileWithHeader(summaryFilePath, header);
        EnsureFileWithHeader(desktopSummaryFilePath, header);
    }

    private void Update()
    {
        if (Time.unscaledTime < nextLookSampleTime)
            return;

        nextLookSampleTime = Time.unscaledTime + Mathf.Max(0.05f, lookSampleInterval);
        SampleLookTargets();
    }

    private void OnApplicationQuit()
    {
        WriteSummaryIfNeeded();
    }

    private void OnDisable()
    {
        if (Instance == this)
            WriteSummaryIfNeeded();
    }

    public void LogStationTriggerEntered(int stationIndex)
    {
        float now = Time.realtimeSinceStartup;
        if (lastStationTriggerTimes.TryGetValue(stationIndex, out float lastTime) &&
            now - lastTime < triggerDebounceSeconds)
        {
            return;
        }

        lastStationTriggerTimes[stationIndex] = now;

        if (stationIndex == 1)
            CompleteStationTransition(0, ref st1ToSt2);
        else if (stationIndex == 2)
            CompleteStationTransition(1, ref st2ToSt3);
        else if (stationIndex == 3)
            CompleteStationTransition(2, ref st3ToSt4);
    }

    public void LogLessonStarted(int stationIndex)
    {
        lessonStartTimes[stationIndex] = Time.realtimeSinceStartup;
    }

    public void LogLessonCompleted(int stationIndex)
    {
        lessonCompleteTimes[stationIndex] = Time.realtimeSinceStartup;
        transitionStartTimes[stationIndex] = Time.realtimeSinceStartup;
        transitionTeleported[stationIndex] = false;
    }

    public void LogTeleportBetweenStations(int fromStationIndex, int toStationIndex)
    {
        totalTeleports++;
        transitionStartTimes[fromStationIndex] = Time.realtimeSinceStartup;
        transitionTeleported[fromStationIndex] = true;

        if (fromStationIndex == 2 && toStationIndex == 3)
            st3ToSt4 = 0f;
    }

    public void LogMapShown()
    {
        if (mapShownTime < 0f)
            mapShownTime = Time.realtimeSinceStartup;
    }

    public void LogMapAnswer(string selectedLocation, bool isCorrect)
    {
        if (mapShownTime < 0f)
            mapShownTime = Time.realtimeSinceStartup;

        if (!mapSolved && !isCorrect)
            mapErrorsBeforeSuccess++;

        if (!mapSolved && isCorrect)
        {
            mapSolved = true;
            mapDuration = Time.realtimeSinceStartup - mapShownTime;
        }
    }

    public void LogStation2ArchitectureSelection(string elementName)
    {
        station2SelectionClicks++;

        if (!string.IsNullOrEmpty(elementName))
            station2UniqueSelections[elementName] = true;
    }

    public void StartWatchingPubblico(Transform target)
    {
        pubblicoWatchTarget = target;
        pubblicoWatchActive = true;
    }

    public void StopWatchingPubblico()
    {
        pubblicoWatchActive = false;
        pubblicoWatchTarget = null;
    }

    public void StartWatchingCapitello(Transform target)
    {
        capitelloWatchTarget = target;
        capitelloWatchActive = true;
    }

    public void StopWatchingCapitello()
    {
        capitelloWatchActive = false;
        capitelloWatchTarget = null;
    }

    public void LogCapitelloMaterialChanged()
    {
        capitelloMaterialChanged = true;
    }

    public void LogTicketShown()
    {
        ticketShownTime = Time.realtimeSinceStartup;
    }

    public void LogTicketGrabbed()
    {
        if (ticketShownTime >= 0f)
            st3ToSt4 = 0f;
    }

    public void LogStation4StepStarted()
    {
        if (station4TaskStartTime < 0f)
            station4TaskStartTime = Time.realtimeSinceStartup;
    }

    public void LogStation4StepCompleted(string stepName)
    {
        if (!string.IsNullOrEmpty(stepName))
            station4CompletedSteps.Add(stepName);

        if (station4CompletedSteps.Count >= 5 && station4TaskStartTime >= 0f && station4TaskDuration < 0f)
            station4TaskDuration = Time.realtimeSinceStartup - station4TaskStartTime;
    }

    public void LogExperienceCompleted()
    {
        sessionCompleted = true;
        WriteSummaryIfNeeded();
    }

    private void CompleteStationTransition(int fromStationIndex, ref float outputField)
    {
        if (!transitionStartTimes.TryGetValue(fromStationIndex, out float startTime))
            return;

        bool teleported = transitionTeleported.TryGetValue(fromStationIndex, out bool wasTeleported) && wasTeleported;
        outputField = teleported ? 0f : Time.realtimeSinceStartup - startTime;
        transitionStartTimes.Remove(fromStationIndex);
        transitionTeleported.Remove(fromStationIndex);
    }

    private void SampleLookTargets()
    {
        Camera targetCamera = Camera.main;
        if (targetCamera == null)
            return;

        if (pubblicoWatchActive && !pubblicoLooked && IsLookingAtTarget(targetCamera.transform, pubblicoWatchTarget))
            pubblicoLooked = true;

        if (capitelloWatchActive && !capitelloLooked && IsLookingAtTarget(targetCamera.transform, capitelloWatchTarget))
            capitelloLooked = true;
    }

    private bool IsLookingAtTarget(Transform viewer, Transform target)
    {
        if (viewer == null || target == null || !target.gameObject.activeInHierarchy)
            return false;

        Vector3 direction = (target.position - viewer.position).normalized;
        float dot = Vector3.Dot(viewer.forward, direction);
        return dot >= lookDotThreshold;
    }

    private void WriteSummaryIfNeeded()
    {
        if (summaryWritten)
            return;

        summaryWritten = true;

        int precisioneMappa = mapSolved ? mapErrorsBeforeSuccess + 1 : 0;
        int selezioneArchitettura = station2UniqueSelections.Count;
        int riselezione = Mathf.Max(0, station2SelectionClicks - selezioneArchitettura);
        int pubblico = pubblicoLooked ? 1 : 0;
        int capitello = capitelloLooked ? 1 : 0;
        int capitelloMateriale = capitelloMaterialChanged ? 1 : 0;

        string line = string.Join(CsvSeparator, new[]
        {
            Escape(sessionId),
            Escape(sessionStartDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)),
            Escape(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)),
            Escape(FormatFloat(Time.realtimeSinceStartup - sessionStartTime)),
            Escape(precisioneMappa.ToString(CultureInfo.InvariantCulture)),
            Escape(FormatFloat(mapDuration)),
            Escape(selezioneArchitettura.ToString(CultureInfo.InvariantCulture)),
            Escape(riselezione.ToString(CultureInfo.InvariantCulture)),
            Escape(pubblico.ToString(CultureInfo.InvariantCulture)),
            Escape(capitello.ToString(CultureInfo.InvariantCulture)),
            Escape(capitelloMateriale.ToString(CultureInfo.InvariantCulture)),
            Escape(FormatFloat(station4TaskDuration)),
            Escape(FormatFloat(st1ToSt2)),
            Escape(FormatFloat(st2ToSt3)),
            Escape(FormatFloat(st3ToSt4)),
            Escape(totalTeleports.ToString(CultureInfo.InvariantCulture))
        });

        AppendLine(summaryFilePath, line);
        AppendLine(desktopSummaryFilePath, line);
    }

    private static void EnsureFileWithHeader(string path, string header)
    {
        if (string.IsNullOrEmpty(path))
            return;

        if (!File.Exists(path))
            File.WriteAllText(path, header, Encoding.UTF8);
    }

    private static void AppendLine(string path, string line)
    {
        if (string.IsNullOrEmpty(path))
            return;

        File.AppendAllText(path, line + Environment.NewLine, Encoding.UTF8);
    }

    private static string FormatFloat(float value)
    {
        if (value < 0f)
            return "";

        return value.ToString("F2", CultureInfo.InvariantCulture);
    }

    private static string Escape(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        string escaped = value.Replace("\"", "\"\"");
        if (escaped.Contains(";") || escaped.Contains(",") || escaped.Contains("\"") || escaped.Contains("\n"))
            return $"\"{escaped}\"";

        return escaped;
    }
}
