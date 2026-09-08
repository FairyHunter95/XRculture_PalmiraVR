using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem.XR;

public class StartSceneManager : MonoBehaviour
{
    [Tooltip("Secondi prima che sia possibile procedere")]
    public float proceedDelay = 5f;

    [Tooltip("Nome della scena del tutorial")]
    public string nextSceneName = "2_TutorialScene";  // Cambia con il nome reale

    private bool canProceed = false;
    private float timer = 0f;


    void Update()
    {
        // Finché non è passato il delay, conta il tempo
        if (!canProceed)
        {
            timer += Time.deltaTime;
            if (timer >= proceedDelay)
            {
                canProceed = true;
            }
            return;
        }

        // Da qui in poi: puoi procedere se un pulsante è stato premuto
        if (AnyButtonPressedThisFrame())
        {
            LoadNextScene();
        }
    }

    bool AnyButtonPressedThisFrame()
    {
        foreach (var device in InputSystem.devices)
        {
            // Solo controller XR (no HMD, no tracker)
            if (device is not XRController)
                continue;

            // Scansiona tutti i controlli del device
            foreach (var control in device.allControls)
            {
                // Prendi SOLO i ButtonControl (no leve, no tracking)
                if (control is ButtonControl button)
                {
                    // Escludiamo pulsanti sintetici
                    if (button.synthetic)
                        continue;

                    // Controlliamo SOLO i pulsanti veri dei controller
                    string name = button.name.ToLower();

                    if (
                        name.Contains("primarybutton") ||       // A / X
                        name.Contains("secondarybutton") ||     // B / Y
                        name.Contains("menubutton") ||          // Menu
                        name.Contains("triggerbutton") ||       // Trigger as button
                        name.Contains("gripbutton") ||          // Grip as button
                        name.Contains("thumbstickclicked")    // Stick click
                       )
                    {
                        if (button.wasPressedThisFrame)
                            return true;
                    }
                }
            }
        }

        return false;
    }

    void LoadNextScene()
    {
        
            SceneManager.LoadScene(nextSceneName);
        
    }
}
