using UnityEngine;

public class GlowPulse : MonoBehaviour
{
    public Material mat;
    public float speed = 2f;
    public float intensity = 2f;

    void Update()
    {
        float emission = Mathf.PingPong(Time.time * speed, intensity);
        mat.SetColor("_EmissionColor", Color.yellow * emission);
    }
}