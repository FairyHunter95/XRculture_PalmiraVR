using System;
using UnityEngine;

public enum LessonEndMode
{
    Manual,     // termina solo quando il controller chiama CompleteLesson()
    Timer,      // termina dopo lessonDuration
    AudioEnd    // termina quando finisce narrationAudio (quando lo avrai)
}

[Serializable]
public class StationTimedEvent
{
    public string name = "Event";

    [Tooltip("Secondi dall'inizio lezione in cui scatta questo evento.")]
    public float time;

    [Tooltip("Durata dell'effetto (0 = istantaneo). Usata solo se revertAfter=true.")]
    public float duration;

    [Tooltip("Oggetto da attivare/disattivare (pannelli, highlight, capitello, ecc.).")]
    public GameObject target;

    [Tooltip("Stato da impostare a 'time' (true=attivo, false=spento).")]
    public bool setActive = true;

    [Tooltip("Se true, dopo 'duration' ritorna allo stato opposto.")]
    public bool revertAfter = false;
}

public class LessonStation : MonoBehaviour
{
    [Header("Setup")]
    public int stationIndex;
    public Transform spawnPoint;
    public Collider triggerZone;

    [Header("Marker (optional)")]
    [Tooltip("Empty 'Marker' dentro la station (VFX + testo lezione).")]
    public GameObject markerRoot;

    [Header("Content (optional)")]
    public GameObject textPanel;
    public GameObject mediaPanel;

    [Header("Lesson End")]
    public LessonEndMode endMode = LessonEndMode.Timer;
    public float lessonDuration;          // placeholder per ora
    public AudioSource narrationAudio;    // lo assegnerai quando avrai gli audio

    [Header("Timed Events")]
    public StationTimedEvent[] events;

    private void Reset()
    {
        spawnPoint = transform.Find("SpawnPoint");

        var tz = transform.Find("TriggerZone");
        if (tz != null) triggerZone = tz.GetComponent<Collider>();
        if (triggerZone != null) triggerZone.isTrigger = true;

        var mk = transform.Find("Marker");
        if (mk != null) markerRoot = mk.gameObject;
    }

    public void SetActiveStation(bool isActive)
    {
        // di default li teniamo spenti e li accendiamo con gli eventi
        if (textPanel) textPanel.SetActive(false);
        if (mediaPanel) mediaPanel.SetActive(false);

        // quando disattivi una station, spegni tutto ciò che potrebbe essere rimasto acceso
        if (!isActive && events != null)
        {
            foreach (var e in events)
            {
                if (e != null && e.target != null)
                    e.target.SetActive(false);
            }
        }
    }
}
