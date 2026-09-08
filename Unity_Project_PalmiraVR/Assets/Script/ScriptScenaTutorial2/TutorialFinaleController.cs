using UnityEngine;
using UnityEngine.SceneManagement;

public class TutorialFinaleController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] GameObject coachingCardRoot;
    [SerializeField] GameObject finalLogosRoot;

    [Header("Scene Transition")]
    [SerializeField] float finalLogosDuration = 6f;
    [SerializeField] string nextSceneName = "3_Introduction";

    bool isTransitionRunning;

    void Awake()
    {
        if (finalLogosRoot != null)
            finalLogosRoot.SetActive(false);
    }

    public void ShowFinalLogos()
    {
        if (isTransitionRunning)
            return;

        isTransitionRunning = true;

        if (coachingCardRoot != null)
            coachingCardRoot.SetActive(false);

        if (finalLogosRoot != null)
            finalLogosRoot.SetActive(true);

        Invoke(nameof(LoadNextScene), finalLogosDuration);
    }

    void LoadNextScene()
    {
        SceneManager.LoadScene(nextSceneName);
    }
}
