using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.VRTemplate
{
    /// <summary>
    /// Controls the steps in the in coaching card.
    /// </summary>
    public class StepManager : MonoBehaviour
    {
        [Serializable]
        class Step
        {
            [SerializeField]
            public GameObject stepObject;

            [SerializeField]
            public string buttonText;
        }

        [SerializeField]
        public TextMeshProUGUI m_StepButtonTextField;

        [SerializeField]
        List<Step> m_StepList = new List<Step>();

        [SerializeField]
        TutorialFinaleController m_TutorialFinaleController;

        int m_CurrentStepIndex = 0;

        public void Next()
        {
            // Nascondo lo step corrente
            m_StepList[m_CurrentStepIndex].stepObject.SetActive(false);

            // Se NON siamo ancora all'ultimo step to vai al prossimo
            if (m_CurrentStepIndex < m_StepList.Count - 1)
            {
                m_CurrentStepIndex++;
                m_StepList[m_CurrentStepIndex].stepObject.SetActive(true);
                m_StepButtonTextField.text = m_StepList[m_CurrentStepIndex].buttonText;
            }
            else
            {
                if (m_TutorialFinaleController != null)
                    m_TutorialFinaleController.ShowFinalLogos();
                else
                    SceneManager.LoadScene("3_Introduction");
            }
        }
    }
}
