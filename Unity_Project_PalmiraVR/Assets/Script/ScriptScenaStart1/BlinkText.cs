using UnityEngine;
using TMPro;

public class BlinkText : MonoBehaviour
{
    public TextMeshProUGUI text;      // Il tuo TMP Text
    public float delay = 5f;          // Secondi prima che appaia
    public float blinkSpeed = 4.0f;   // Velocità dell’effetto blink

    private bool startBlink = false;
    private Color originalColor;

    void Start()
    {
        if (text != null)
        {
            // Salvo il colore originale
            originalColor = text.color;

            // Lo rendo invisibile all’inizio
            Color c = originalColor;
            c.a = 0f;
            text.color = c;

            // Avvia coroutine per la comparsa
            StartCoroutine(DelayBlink());
        }
    }

    private System.Collections.IEnumerator DelayBlink()
    {
        yield return new WaitForSeconds(delay);

        // Ora il blink può iniziare
        startBlink = true;
    }

    void Update()
    {
        if (startBlink && text != null)
        {
            Color c = text.color;
            c.a = (Mathf.Sin(Time.time * blinkSpeed) + 1f) / 2f; // 0 to 1
            text.color = c;
        }
    }
}
