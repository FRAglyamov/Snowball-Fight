using UnityEngine;
using UnityEngine.Rendering.Universal;

public class SimpleLightFlicker : MonoBehaviour
{
    private Light2D _light;
    [SerializeField]
    private float minInstensity = 0.5f;
    [SerializeField]
    private float intensityAndFrequence = 2f;
    [SerializeField]
    private float intensityDivider = 2f;

    void Start()
    {
        _light = GetComponent<Light2D>();
    }

    void Update()
    {
        _light.intensity = Mathf.PingPong(Time.time, intensityAndFrequence) / intensityDivider + minInstensity;
    }
}
