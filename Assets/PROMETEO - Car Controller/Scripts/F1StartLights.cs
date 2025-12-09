using UnityEngine;

public class F1StartLights : MonoBehaviour
{
    [Header("Light Settings")]
    public GameObject[] lights = new GameObject[5]; // 5 red lights
    public Material lightOnMaterial; // Red emissive material
    public Material lightOffMaterial; // Dark material
    
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip lightOnSound;
    public AudioClip lightsOutSound;

    private int currentLights = 0;

    void Start()
    {
        // Initialize all lights to OFF
        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i] != null)
            {
                SetLightState(i, false);
            }
        }
    }

    // Called by F1RaceManager during countdown
    public void SetLightIndex(int index)
    {
        if (index >= 0 && index <= 5)
        {
            currentLights = index;

            if (index == 0)
            {
                // All lights OFF initially
                TurnAllLightsOff();
            }
            else if (index <= 5)
            {
                // Turn on lights 1-5 sequentially
                TurnOnLight(index - 1);
            }

            if (index == 5)
            {
                // All lights go OUT = GO!
                Invoke(nameof(LightsOut), 0.1f);
            }
        }
    }

    void TurnOnLight(int lightIndex)
    {
        if (lightIndex < lights.Length && lights[lightIndex] != null)
        {
            SetLightState(lightIndex, true);
            
            // Play sound
            if (audioSource != null && lightOnSound != null)
            {
                audioSource.PlayOneShot(lightOnSound);
            }
        }
    }

    void TurnAllLightsOff()
    {
        for (int i = 0; i < lights.Length; i++)
        {
            SetLightState(i, false);
        }
    }

    void LightsOut()
    {
        TurnAllLightsOff();
        
        // Play GO sound
        if (audioSource != null && lightsOutSound != null)
        {
            audioSource.PlayOneShot(lightsOutSound);
        }
    }

    void SetLightState(int index, bool isOn)
    {
        if (index >= lights.Length || lights[index] == null) return;

        Renderer renderer = lights[index].GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = isOn ? lightOnMaterial : lightOffMaterial;
        }

        // Also control light component if it exists
        Light lightComponent = lights[index].GetComponent<Light>();
        if (lightComponent != null)
        {
            lightComponent.enabled = isOn;
        }
    }

    // Manual testing in Editor
    [ContextMenu("Test Light Sequence")]
    void TestSequence()
    {
        StartCoroutine(TestLightSequenceCoroutine());
    }

    System.Collections.IEnumerator TestLightSequenceCoroutine()
    {
        TurnAllLightsOff();
        yield return new WaitForSeconds(0.5f);

        for (int i = 0; i < 5; i++)
        {
            TurnOnLight(i);
            yield return new WaitForSeconds(1f);
        }

        yield return new WaitForSeconds(0.5f);
        LightsOut();
    }
}
