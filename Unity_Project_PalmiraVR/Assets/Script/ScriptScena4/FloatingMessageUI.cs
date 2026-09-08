using System.Collections;
using TMPro;
using UnityEngine;

public class FloatingMessageUI : MonoBehaviour
{
    [Header("References")]
    public TextMeshPro messageText;
    //public GameObject panelRoot;

    [Header("Timing")]
    public float defaultDuration = 2.5f;

    Coroutine routine;

  

    public void Show(string msg, float duration = -1f)
    {
        if (routine != null) StopCoroutine(routine);

        if (duration <= 0f) duration = defaultDuration;

       // panelRoot.SetActive(true);
        messageText.text = msg;

        routine = StartCoroutine(AutoHide(duration));
    }

    public void ShowPersistent(string msg)
    {
        if (routine != null) StopCoroutine(routine);

      //  panelRoot.SetActive(true);
        messageText.text = msg;
    }

    

    public void ShowCountdown(string prefix, int seconds)
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(CountdownRoutine(prefix, seconds));
    }

    IEnumerator AutoHide(float duration)
    {
        yield return new WaitForSeconds(duration);
       // panelRoot.SetActive(false);
        routine = null;
    }

    IEnumerator CountdownRoutine(string prefix, int seconds)
    {
      //  panelRoot.SetActive(true);

        for (int t = seconds; t > 0; t--)
        {
            messageText.text = $"{prefix} {t}…";
            yield return new WaitForSeconds(1f);
        }

        //panelRoot.SetActive(false);
        routine = null;
    }
}
