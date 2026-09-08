using UnityEngine;

public class StationTrigger : MonoBehaviour
{
    public LessonStation station;

    private void Reset()
    {
        station = GetComponentInParent<LessonStation>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // Filtra: accetta CharacterController (XR Origin)
        if (other.GetComponent<CharacterController>() == null)
            return;

        if (station == null)
            station = GetComponentInParent<LessonStation>();

        LessonFlowController.Instance?.OnStationEntered(station);
    }
}
