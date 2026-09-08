using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class ExplorePalmiraButton : MonoBehaviour
{
    [Header("Scene")]
    public string nextSceneName = "4_InsidePalmira";

    [Header("Optional")]
    public GameObject panelRoot;

    private XRSimpleInteractable interactable;

    private void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();
    }

    private void OnEnable()
    {
        // Grab / Select
        interactable.selectEntered.AddListener(OnSelect);

        // Trigger / Activate
        interactable.activated.AddListener(OnActivate);
    }

    private void OnDisable()
    {
        interactable.selectEntered.RemoveListener(OnSelect);
        interactable.activated.RemoveListener(OnActivate);
    }

    private void OnSelect(SelectEnterEventArgs args)
    {
        Proceed();
    }

    private void OnActivate(ActivateEventArgs args)
    {
        Proceed();
    }

    private void Proceed()
    {
        Debug.Log("Esplora Palmira premuto");

        if (panelRoot != null)
            panelRoot.SetActive(false);

        SceneManager.LoadScene(nextSceneName);
    }
}
