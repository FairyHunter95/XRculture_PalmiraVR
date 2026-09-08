using UnityEngine;

public class StationTriggerDebug : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<CharacterController>() == null)
            return;

        Debug.Log("Entrato in " + transform.parent.name);
    }
}
