using UnityEngine;
using System.Collections;

public enum PowerUpType
{
    DRS_Boost,
    Temporary_Shield,
    Tyre_Grip_Boost
}

public class PowerUp : MonoBehaviour
{
    public PowerUpType type;
    public float duration = 5f;
    public float respawnDelay = 10f;

    private bool isCollected = false;
    private MeshRenderer[] meshRenderers;
    private Collider powerUpCollider;

    private void Awake()
    {
        // Cache references
        meshRenderers = GetComponentsInChildren<MeshRenderer>();
        powerUpCollider = GetComponent<Collider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // Prevent collecting the same power-up multiple times before respawn
        if (isCollected) return;

        CarPowerUpManager carPowerUpManager = other.GetComponentInParent<CarPowerUpManager>();
        if (carPowerUpManager != null)
        {
            // Always activate - CarPowerUpManager will handle refreshing the timer
            carPowerUpManager.ActivatePowerUp(type, duration);

            // Mark as collected
            isCollected = true;

            Debug.Log($"PowerUp: {gameObject.name} collected by {other.transform.root.name}. Will respawn in {respawnDelay} seconds.");

            // Hide the power-up visually and disable collision
            HidePowerUp();

            // Start respawn coroutine (GameObject stays active so coroutine works)
            StartCoroutine(RespawnAfterDelay());
        }
    }

    private void HidePowerUp()
    {
        // Disable all mesh renderers to make it invisible
        foreach (MeshRenderer mr in meshRenderers)
        {
            if (mr != null)
                mr.enabled = false;
        }

        // Disable collider so it can't be collected again
        if (powerUpCollider != null)
            powerUpCollider.enabled = false;
    }

    private void ShowPowerUp()
    {
        // Re-enable all mesh renderers to make it visible
        foreach (MeshRenderer mr in meshRenderers)
        {
            if (mr != null)
                mr.enabled = true;
        }

        // Re-enable collider so it can be collected again
        if (powerUpCollider != null)
            powerUpCollider.enabled = true;
    }

    private IEnumerator RespawnAfterDelay()
    {
        Debug.Log($"PowerUp: {gameObject.name} starting {respawnDelay}s respawn timer...");

        yield return new WaitForSeconds(respawnDelay);

        // Show the power-up again
        ShowPowerUp();
        isCollected = false;

        Debug.Log($"PowerUp: {gameObject.name} RESPAWNED and is now collectible again!");
    }
}