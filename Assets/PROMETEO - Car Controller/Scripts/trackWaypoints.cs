using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class TrackWaypoints : MonoBehaviour
{
    public Color lineColor = Color.white;
    public float sphereRadius = 0.2f;   // adjustable in Inspector
    public float tickLength = 0.5f;     // length of direction tick
    public List<Transform> nodes = new List<Transform>();
    public bool showGizmosInPlayMode = true; // New toggle for runtime visibility

    private void Awake()
    {
        PopulateNodes();
    }

    private void PopulateNodes()
    {
        nodes.Clear(); // Clear existing nodes
        Transform[] path = GetComponentsInChildren<Transform>();
        for (int i = 1; i < path.Length; i++)
        {
            nodes.Add(path[i]);
        }
    }

    // Draw gizmos in editor and during play mode if enabled
    private void OnDrawGizmos()
    {
        if (!showGizmosInPlayMode && !Application.isEditor) return; // Only draw in editor or if enabled for play mode

        // Ensure nodes are populated for visualization
        if (nodes.Count == 0)
        {
            PopulateNodes();
        }

        Gizmos.color = lineColor;

        for (int i = 0; i < nodes.Count; i++)
        {
            Vector3 currentWaypoint = nodes[i].position;
            Vector3 previousWaypoint = (i != 0) ? nodes[i - 1].position : nodes[nodes.Count - 1].position;

            // Draw thicker line in Scene view
#if UNITY_EDITOR
            UnityEditor.Handles.color = lineColor;
            UnityEditor.Handles.DrawAAPolyLine(4, previousWaypoint, currentWaypoint); // thickness = 4
#endif

            // Draw sphere at each waypoint
            Gizmos.DrawSphere(currentWaypoint, sphereRadius);

            // Draw a short tick to indicate direction
            Vector3 direction = (currentWaypoint - previousWaypoint).normalized;
            Vector3 tickEnd = currentWaypoint + direction * tickLength;
            Gizmos.DrawLine(currentWaypoint, tickEnd);
        }
    }

    // Keep OnDrawGizmosSelected for editor-only selection-based drawing if needed,
    // but the main drawing logic is now in OnDrawGizmos.
    private void OnDrawGizmosSelected()
    {
        // This can be used for additional debug info when selected in editor
        // For now, it will just call the main OnDrawGizmos to avoid duplicate code
        OnDrawGizmos();
    }
}
