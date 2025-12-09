using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CarPowerUpManager : MonoBehaviour
{
    [Header("References")]
    public PrometeoCarController carController;

    [Header("Power-Up UI (Player Only)")]
    public GameObject powerUpUIContainer;
    public Image drsIcon;
    public Text drsTimer;
    public Image shieldIcon;
    public Text shieldTimer;
    public Image gripIcon;
    public Text gripTimer;

    [Header("Power-Up Visual Effects")]
    public GameObject drsEffectPrefab;
    public GameObject shieldEffectPrefab;
    public GameObject gripEffectPrefab;

    private GameObject activeDRSEffect;
    private GameObject activeShieldEffect;
    private GameObject activeGripEffect;

    // Power-up states
    private bool isDRSActive = false;
    private bool isShieldActive = false;
    private bool isGripActive = false;

    // Coroutine references for stopping/restarting
    private Coroutine drsCoroutine;
    private Coroutine shieldCoroutine;
    private Coroutine gripCoroutine;

    // Original car values (NEVER MODIFIED)
    private float originalMaxForwardSpeed;
    private float originalAccelerationMultiplier;
    private float originalDownforceCoefficient;
    private float originalAntiRollForceMultiplier;

    // Power-up effect multipliers/values
    [Header("DRS Boost Settings")]
    public float drsSpeedMultiplier = 1.2f;
    public float drsDownforceReduction = 0.5f;

    [Header("Temporary Shield Settings")]
    public float shieldCollisionReduction = 0.1f;

    [Header("Tyre Grip Boost Settings")]
    public float gripDownforceMultiplier = 1.5f;
    public float gripAntiRollMultiplier = 1.2f;

    void Start()
    {
        carController = GetComponent<PrometeoCarController>();

        // Store original values ONCE and never modify them
        if (carController != null)
        {
            originalMaxForwardSpeed = carController.maxForwardSpeed;
            originalAccelerationMultiplier = carController.accelerationMultiplier;
            originalDownforceCoefficient = carController.downforceCoefficient;
            originalAntiRollForceMultiplier = carController.antiRollForceMultiplier;
        }

        // Hide all power-up UI at start
        if (powerUpUIContainer != null)
        {
            if (drsIcon != null) drsIcon.gameObject.SetActive(false);
            if (shieldIcon != null) shieldIcon.gameObject.SetActive(false);
            if (gripIcon != null) gripIcon.gameObject.SetActive(false);
        }
    }

    public void ActivatePowerUp(PowerUpType type, float duration)
    {
        switch (type)
        {
            case PowerUpType.DRS_Boost:
                // Stop existing coroutine if active, then restart with new duration
                if (drsCoroutine != null)
                {
                    StopCoroutine(drsCoroutine);
                }
                drsCoroutine = StartCoroutine(DRSBoostCoroutine(duration));
                break;
            case PowerUpType.Temporary_Shield:
                if (shieldCoroutine != null)
                {
                    StopCoroutine(shieldCoroutine);
                }
                shieldCoroutine = StartCoroutine(TemporaryShieldCoroutine(duration));
                break;
            case PowerUpType.Tyre_Grip_Boost:
                if (gripCoroutine != null)
                {
                    StopCoroutine(gripCoroutine);
                }
                gripCoroutine = StartCoroutine(TyreGripBoostCoroutine(duration));
                break;
        }
    }

    IEnumerator DRSBoostCoroutine(float duration)
    {
        // If not already active, initialize effects
        if (!isDRSActive)
        {
            isDRSActive = true;
            RecalculateCarProperties();

            // Visual effect
            if (drsEffectPrefab != null && activeDRSEffect == null)
            {
                activeDRSEffect = Instantiate(drsEffectPrefab, carController.transform);
            }
        }

        // Update UI timer
        if (powerUpUIContainer != null && drsIcon != null && drsTimer != null)
        {
            drsIcon.gameObject.SetActive(true);
            float timer = duration;
            while (timer > 0)
            {
                drsTimer.text = timer.ToString("F1");
                yield return null;
                timer -= Time.deltaTime;
            }
            drsIcon.gameObject.SetActive(false);
        }
        else
        {
            yield return new WaitForSeconds(duration);
        }

        // Revert effects
        isDRSActive = false;
        RecalculateCarProperties();

        // Destroy visual effect
        if (activeDRSEffect != null)
        {
            Destroy(activeDRSEffect);
            activeDRSEffect = null;
        }

        drsCoroutine = null;
    }

    IEnumerator TemporaryShieldCoroutine(float duration)
    {
        if (!isShieldActive)
        {
            isShieldActive = true;
            RecalculateCarProperties();

            if (shieldEffectPrefab != null && activeShieldEffect == null)
            {
                activeShieldEffect = Instantiate(shieldEffectPrefab, carController.transform);
            }
        }

        if (powerUpUIContainer != null && shieldIcon != null && shieldTimer != null)
        {
            shieldIcon.gameObject.SetActive(true);
            float timer = duration;
            while (timer > 0)
            {
                shieldTimer.text = timer.ToString("F1");
                yield return null;
                timer -= Time.deltaTime;
            }
            shieldIcon.gameObject.SetActive(false);
        }
        else
        {
            yield return new WaitForSeconds(duration);
        }

        isShieldActive = false;
        RecalculateCarProperties();

        if (activeShieldEffect != null)
        {
            Destroy(activeShieldEffect);
            activeShieldEffect = null;
        }

        shieldCoroutine = null;
    }

    IEnumerator TyreGripBoostCoroutine(float duration)
    {
        if (!isGripActive)
        {
            isGripActive = true;
            RecalculateCarProperties();

            if (gripEffectPrefab != null && activeGripEffect == null)
            {
                activeGripEffect = Instantiate(gripEffectPrefab, carController.transform);
            }
        }

        if (powerUpUIContainer != null && gripIcon != null && gripTimer != null)
        {
            gripIcon.gameObject.SetActive(true);
            float timer = duration;
            while (timer > 0)
            {
                gripTimer.text = timer.ToString("F1");
                yield return null;
                timer -= Time.deltaTime;
            }
            gripIcon.gameObject.SetActive(false);
        }
        else
        {
            yield return new WaitForSeconds(duration);
        }

        isGripActive = false;
        RecalculateCarProperties();

        if (activeGripEffect != null)
        {
            Destroy(activeGripEffect);
            activeGripEffect = null;
        }

        gripCoroutine = null;
    }

    // Public methods for AI to query power-up status
    public bool IsDRSActive() { return isDRSActive; }
    public bool IsShieldActive() { return isShieldActive; }
    public bool IsGripActive() { return isGripActive; }

    private void RecalculateCarProperties()
    {
        float newMaxForwardSpeed = originalMaxForwardSpeed;
        float newAccelerationMultiplier = originalAccelerationMultiplier;
        float newDownforceCoefficient = originalDownforceCoefficient;
        float newAntiRollForceMultiplier = originalAntiRollForceMultiplier;
        bool newCollisionReduced = false;

        if (isDRSActive)
        {
            newMaxForwardSpeed *= drsSpeedMultiplier;
            newDownforceCoefficient *= drsDownforceReduction;
        }

        if (isGripActive)
        {
            if (!isDRSActive)
            {
                newDownforceCoefficient *= gripDownforceMultiplier;
            }
            newAntiRollForceMultiplier *= gripAntiRollMultiplier;
        }

        if (isShieldActive)
        {
            newCollisionReduced = true;
        }

        if (carController != null)
        {
            carController.SetMaxForwardSpeed(newMaxForwardSpeed);
            carController.SetAccelerationMultiplier(newAccelerationMultiplier);
            carController.SetDownforceCoefficient(newDownforceCoefficient);
            carController.SetAntiRollForceMultiplier(newAntiRollForceMultiplier);
            carController.SetCollisionReduced(newCollisionReduced);
        }
    }
}