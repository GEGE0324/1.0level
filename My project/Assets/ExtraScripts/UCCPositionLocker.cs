using UnityEngine;

public class UCCPositionLocker : MonoBehaviour
{
    private Vector3 lockedPosition;
    private Quaternion lockedRotation;
    private bool isLocked = false;

    public void Lock(Vector3 position, Quaternion rotation)
    {
        lockedPosition = position;
        lockedRotation = rotation;
        isLocked = true;
    }

    public void Unlock()
    {
        isLocked = false;
    }

    public void UpdateRotation(Quaternion rotation)
    {
        lockedRotation = rotation;
    }

    // Use LateUpdate to override animation root motion and UCC physics
    void LateUpdate()
    {
        if (isLocked)
        {
            transform.position = lockedPosition;
            // Force rotation lock to override any other system
            transform.rotation = lockedRotation; 
        }
    }
}
