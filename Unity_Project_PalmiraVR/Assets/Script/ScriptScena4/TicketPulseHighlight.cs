using UnityEngine;
using UnityEngine.UI;

public class TicketPulseHighlight : MonoBehaviour
{
    [Header("Scale Pulse")]
    public bool pulseScale = true;
    public float pulseSpeed = 2f;
    public float minScaleMultiplier = 1f;
    public float maxScaleMultiplier = 1.08f;

    [Header("Color Pulse")]
    public bool pulseColor = true;
    public Graphic targetGraphic;
    public Color minColor = new Color(1f, 1f, 1f, 0.9f);
    public Color maxColor = new Color(1f, 0.96f, 0.75f, 1f);

    private Vector3 initialScale;

    private void Awake()
    {
        initialScale = transform.localScale;

        if (targetGraphic == null)
            targetGraphic = GetComponentInChildren<Graphic>(true);
    }

    private void OnEnable()
    {
        initialScale = transform.localScale;
    }

    private void OnDisable()
    {
        transform.localScale = initialScale;

        if (targetGraphic != null)
            targetGraphic.color = minColor;
    }

    private void Update()
    {
        float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;

        if (pulseScale)
        {
            float scaleMultiplier = Mathf.Lerp(minScaleMultiplier, maxScaleMultiplier, t);
            transform.localScale = initialScale * scaleMultiplier;
        }

        if (pulseColor && targetGraphic != null)
            targetGraphic.color = Color.Lerp(minColor, maxColor, t);
    }
}
