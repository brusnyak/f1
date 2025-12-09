using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class F1Checkpoint : MonoBehaviour
{
    [Header("Checkpoint Settings")]
    public int checkpointNumber = 0; // 0 = start/finish line
    public bool isStartFinish = false;

    [Header("Debug")]
    public bool showGizmo = true;
    public Color gizmoColor = Color.green;

    private BoxCollider triggerCollider;

    void Start()
    {
        // Setup trigger collider
        triggerCollider = GetComponent<BoxCollider>();
        triggerCollider.isTrigger = true;

        // Color code: green = start/finish, yellow = checkpoints
        if (isStartFinish)
        {
            gizmoColor = Color.green;
        }
        else
        {
            gizmoColor = Color.yellow;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Check if it's a player car
        PrometeoCarController car = other.GetComponentInParent<PrometeoCarController>();
        if (car != null)
        {
            // Notify race manager
            if (F1RaceManager.Instance != null)
            {
                F1RaceManager.Instance.CarPassedCheckpoint(other.transform.root, checkpointNumber);
            }

            Debug.Log($"{other.transform.root.name} passed checkpoint {checkpointNumber}");
        }

    }


    // Visual representation in Scene view
    void OnDrawGizmos()
    {
        if (!showGizmo) return;

        BoxCollider col = GetComponent<BoxCollider>();
        if (col != null)
        {
            Gizmos.color = gizmoColor;
            Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.matrix = rotationMatrix;
            Gizmos.DrawWireCube(col.center, col.size);
            
            // Draw checkpoint number
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2, 
                isStartFinish ? "START/FINISH" : $"CP {checkpointNumber}");
            #endif
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!showGizmo) return;

        BoxCollider col = GetComponent<BoxCollider>();
        if (col != null)
        {
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.3f);
            Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.matrix = rotationMatrix;
            Gizmos.DrawCube(col.center, col.size);
        }
    }
}
