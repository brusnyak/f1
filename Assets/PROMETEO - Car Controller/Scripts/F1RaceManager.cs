using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class F1RaceManager : MonoBehaviour
{
    public static F1RaceManager Instance { get; private set; }

    [Header("Race Settings")]
    public int totalLaps = 3;
    public int numberOfAICars = 3;
    public GameObject aiCarPrefab;

    public enum RaceState { Countdown, Racing, Finished }

    [Header("Race State")]
    public RaceState currentState = RaceState.Countdown;

    [Header("Cars")]
    public PrometeoCarController playerCar;
    public List<Transform> allCars = new List<Transform>();
    public List<PrometeoCarController> aiCarControllers = new List<PrometeoCarController>();

    [Header("Countdown")]
    public float countdownDuration = 5f;
    private float countdownTimer = 0f;
    private int currentLightIndex = 0;

    [Header("Race Data")]
    public Dictionary<Transform, int> carLaps = new Dictionary<Transform, int>();
    public Dictionary<Transform, int> carCheckpoints = new Dictionary<Transform, int>();
    public Dictionary<Transform, float> carLapTimes = new Dictionary<Transform, float>();
    public Dictionary<Transform, float> carTotalRaceTimes = new Dictionary<Transform, float>();
    public Dictionary<Transform, float> carCurrentLapStartTimes = new Dictionary<Transform, float>();
    public Dictionary<Transform, float> carDistanceToNextCheckpoint = new Dictionary<Transform, float>(); // Track distance to next checkpoint
    public Dictionary<Transform, int> carFinishPosition = new Dictionary<Transform, int>(); // Track finish order

    // Power-up Respawn Management
    private Dictionary<GameObject, int> collectedPowerUps = new Dictionary<GameObject, int>(); // PowerUp GameObject -> Lap collected
    public int powerUpRespawnLaps = 1; // Number of laps after which a power-up respawns

    private float raceStartTime;
    private int nextFinishPosition = 1; // Track finish order
    private List<Transform> checkpoints = new List<Transform>(); // Store checkpoint positions

    [Header("UI References")]
    public Text countdownText;
    public Text lapText;
    public Text positionText;
    public Text timerText;
    [Space(10)]
    public GameObject inGameUIContainer;
    public GameObject dimmingPanel;
    public GameObject resultsPanel;
    public Text resultsTitleText;
    public Transform resultsContentParent;
    public GameObject resultEntryPrefab;
    public Button restartButton;

    void Awake()
    {
        if (inGameUIContainer != null) inGameUIContainer.SetActive(false);
        if (dimmingPanel != null) dimmingPanel.SetActive(false);
        if (resultsPanel != null) resultsPanel.SetActive(false);

        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // Find all checkpoints in the scene
        F1Checkpoint[] checkpointScripts = FindObjectsByType<F1Checkpoint>(FindObjectsSortMode.None);
        foreach (F1Checkpoint cp in checkpointScripts)
        {
            checkpoints.Add(cp.transform);
        }
        // Sort checkpoints by their checkpoint number
        checkpoints.Sort((a, b) =>
        {
            int numA = a.GetComponent<F1Checkpoint>().checkpointNumber;
            int numB = b.GetComponent<F1Checkpoint>().checkpointNumber;
            return numA.CompareTo(numB);
        });

        InitializeRace();
    }

    void InitializeRace()
    {
        if (inGameUIContainer != null) inGameUIContainer.SetActive(false);
        if (dimmingPanel != null) dimmingPanel.SetActive(false);
        if (resultsPanel != null) resultsPanel.SetActive(false);

        raceStartTime = 0f;
        nextFinishPosition = 1; // Reset finish position counter

        allCars.Clear();
        aiCarControllers.Clear();

        PrometeoCarController[] allCarControllersInScene = FindObjectsByType<PrometeoCarController>(FindObjectsSortMode.None);

        foreach (PrometeoCarController carController in allCarControllersInScene)
        {
            if (carController == playerCar)
            {
                allCars.Add(playerCar.transform);
                playerCar.GetComponent<PrometeoCarController>().enabled = false;
            }
            else
            {
                aiCarControllers.Add(carController);
                allCars.Add(carController.transform);
                carController.enabled = false;

                inputManager aiInputManager = carController.GetComponent<inputManager>();
                if (aiInputManager != null)
                {
                    aiInputManager.driveController = inputManager.driver.AI;
                }
            }
        }

        int currentAICarCount = aiCarControllers.Count;
        if (aiCarPrefab != null && currentAICarCount < numberOfAICars)
        {
            for (int i = 0; i < numberOfAICars - currentAICarCount; i++)
            {
                if (playerCar != null)
                {
                    Vector3 spawnPosition = playerCar.transform.position - playerCar.transform.forward * (5f * (currentAICarCount + i + 1));
                    Quaternion spawnRotation = playerCar.transform.rotation;
                    GameObject aiCarGO = Instantiate(aiCarPrefab, spawnPosition, spawnRotation);
                    aiCarGO.name = $"AI_Car_{currentAICarCount + i + 1}";

                    PrometeoCarController aiController = aiCarGO.GetComponent<PrometeoCarController>();
                    if (aiController != null)
                    {
                        aiCarControllers.Add(aiController);
                        allCars.Add(aiCarGO.transform);
                        aiController.enabled = false;

                        inputManager aiInputManager = aiCarGO.GetComponent<inputManager>();
                        if (aiInputManager != null)
                        {
                            aiInputManager.driveController = inputManager.driver.AI;
                        }
                    }
                }
            }
        }

        foreach (Transform car in allCars)
        {
            if (!carLaps.ContainsKey(car)) carLaps[car] = 0;
            if (!carCheckpoints.ContainsKey(car)) carCheckpoints[car] = 0;
            if (!carLapTimes.ContainsKey(car)) carLapTimes[car] = 0f;
            if (!carTotalRaceTimes.ContainsKey(car)) carTotalRaceTimes[car] = 0f;
            if (!carCurrentLapStartTimes.ContainsKey(car)) carCurrentLapStartTimes[car] = 0f;
            if (!carDistanceToNextCheckpoint.ContainsKey(car)) carDistanceToNextCheckpoint[car] = 0f;
            if (!carFinishPosition.ContainsKey(car)) carFinishPosition[car] = 0; // 0 means hasn't finished
        }

        collectedPowerUps.Clear(); // Clear collected power-ups for a new race

        if (inGameUIContainer != null) inGameUIContainer.SetActive(true);

        currentState = RaceState.Countdown;
        countdownTimer = countdownDuration;
        currentLightIndex = 0;
    }

    // Public method for PowerUp script to call when collected
    public void PowerUpCollected(GameObject powerUpGameObject)
    {
        if (!collectedPowerUps.ContainsKey(powerUpGameObject))
        {
            // Store the current lap of the player car when the power-up is collected
            // Assuming power-ups are collected by the player for respawn tracking
            if (playerCar != null && carLaps.ContainsKey(playerCar.transform))
            {
                collectedPowerUps[powerUpGameObject] = carLaps[playerCar.transform];
                Debug.Log($"PowerUp {powerUpGameObject.name} collected on lap {carLaps[playerCar.transform]}.");
            }
            else
            {
                // If not collected by player, or player car not found, track with current race lap (if applicable)
                // For simplicity, we'll assume player car's lap for now.
                collectedPowerUps[powerUpGameObject] = 0; // Default to lap 0 if player car not ready
                Debug.LogWarning($"PowerUp {powerUpGameObject.name} collected, but player car lap not available. Tracking as lap 0.");
            }
        }
    }

    void Update()
    {
        // Update distance to next checkpoint for all cars (for position calculation)
        UpdateCarDistances();

        switch (currentState)
        {
            case RaceState.Countdown:
                HandleCountdown();
                break;
            case RaceState.Racing:
                HandleRacing();
                break;
            case RaceState.Finished:
                HandleFinished();
                break;
        }
    }

    void UpdateCarDistances()
    {
        foreach (Transform car in allCars)
        {
            if (checkpoints.Count > 0)
            {
                int nextCheckpointIndex = carCheckpoints[car];
                Transform nextCheckpoint = checkpoints[nextCheckpointIndex % checkpoints.Count];
                carDistanceToNextCheckpoint[car] = Vector3.Distance(car.position, nextCheckpoint.position);
            }
        }
    }

    void HandleCountdown()
    {
        countdownTimer -= Time.deltaTime;

        int newLightIndex = Mathf.FloorToInt(countdownDuration - countdownTimer);
        if (newLightIndex != currentLightIndex && newLightIndex <= 5)
        {
            currentLightIndex = newLightIndex;
            UpdateCountdownLights(currentLightIndex);
        }

        if (countdownText != null)
        {
            if (countdownTimer > 0)
            {
                countdownText.text = Mathf.Ceil(countdownTimer).ToString();
            }
            else
            {
                countdownText.text = "GO!";
            }
        }

        if (countdownTimer <= 0)
        {
            if (currentState == RaceState.Countdown)
            {
                currentState = RaceState.Racing;
                StartRace();
            }
        }
    }

    void UpdateCountdownLights(int lightIndex)
    {
        GameObject lights = GameObject.Find("interlagos_startlights002");
        if (lights != null)
        {
            F1StartLights lightsScript = lights.GetComponent<F1StartLights>();
            if (lightsScript != null)
            {
                lightsScript.SetLightIndex(lightIndex);
            }
        }
    }

    void StartRace()
    {
        if (raceStartTime == 0f)
        {
            raceStartTime = Time.time;

            if (playerCar != null)
            {
                playerCar.GetComponent<PrometeoCarController>().enabled = true;
                playerCar.GetComponent<inputManager>().SetRaceStarted(true);
            }

            foreach (PrometeoCarController aiController in aiCarControllers)
            {
                if (aiController != null)
                {
                    aiController.enabled = true;
                    aiController.GetComponent<inputManager>().SetRaceStarted(true);
                }
            }

            if (restartButton != null)
            {
                restartButton.onClick.RemoveAllListeners();
                restartButton.onClick.AddListener(RestartRace);
            }

            if (countdownText != null)
            {
                Invoke(nameof(HideCountdownText), 1f);
            }

            foreach (Transform car in allCars)
            {
                carLaps[car] = 0;
                carCheckpoints[car] = 0;
                carLapTimes[car] = 0f;
                carTotalRaceTimes[car] = 0f;
                carCurrentLapStartTimes[car] = Time.time;
                carFinishPosition[car] = 0;
            }
        }
    }

    void RestartRace()
    {
        SceneManager.LoadScene("F1 Demo");
        SceneManager.LoadScene("Track", LoadSceneMode.Additive);
    }

    void HideCountdownText()
    {
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
        }
    }

    void HandleRacing()
    {
        UpdateUI();
        CheckRaceCompletion();
    }

    void UpdateUI()
    {
        if (lapText != null && playerCar != null)
        {
            if (currentState == RaceState.Racing || currentState == RaceState.Finished)
            {
                int currentLap = carLaps[playerCar.transform];
                lapText.text = $"Lap {Mathf.Min(currentLap + 1, totalLaps)}/{totalLaps}";
            }
        }

        // Update position (works during racing and after finished)
        if (positionText != null && playerCar != null)
        {
            int position = CalculatePosition(playerCar.transform);
            positionText.text = GetPositionText(position);
        }

        // Update timer
        if (timerText != null && currentState == RaceState.Racing)
        {
            if (playerCar != null && carCurrentLapStartTimes.ContainsKey(playerCar.transform))
            {
                float currentTime = Time.time - carCurrentLapStartTimes[playerCar.transform];
                timerText.text = FormatTime(currentTime);
            }
        }
    }

    int CalculatePosition(Transform car)
    {
        int position = 1;

        // If car has finished, use their finish position
        if (carFinishPosition.ContainsKey(car) && carFinishPosition[car] > 0)
        {
            return carFinishPosition[car];
        }

        int thisLap = carLaps[car];
        int thisCheckpoint = carCheckpoints[car];
        float thisDistance = carDistanceToNextCheckpoint[car];

        List<Transform> sortedCars = new List<Transform>(allCars);
        sortedCars.Sort((a, b) =>
        {
            // If both cars have finished, sort by finish position
            if (carFinishPosition.ContainsKey(a) && carFinishPosition[a] > 0 && carFinishPosition.ContainsKey(b) && carFinishPosition[b] > 0)
            {
                return carFinishPosition[a].CompareTo(carFinishPosition[b]);
            }
            // If only one car has finished, finished car is ahead
            if (carFinishPosition.ContainsKey(a) && carFinishPosition[a] > 0) return -1;
            if (carFinishPosition.ContainsKey(b) && carFinishPosition[b] > 0) return 1;

            int lapA = carLaps.ContainsKey(a) ? carLaps[a] : 0;
            int checkpointA = carCheckpoints.ContainsKey(a) ? carCheckpoints[a] : 0;
            float distanceA = carDistanceToNextCheckpoint.ContainsKey(a) ? carDistanceToNextCheckpoint[a] : float.MaxValue;

            int lapB = carLaps.ContainsKey(b) ? carLaps[b] : 0;
            int checkpointB = carCheckpoints.ContainsKey(b) ? carCheckpoints[b] : 0;
            float distanceB = carDistanceToNextCheckpoint.ContainsKey(b) ? carDistanceToNextCheckpoint[b] : float.MaxValue;

            // Primary sort by laps (descending - more laps = better)
            if (lapA != lapB)
            {
                return lapB.CompareTo(lapA);
            }
            // Secondary sort by checkpoints (descending - more checkpoints = better)
            if (checkpointA != checkpointB)
            {
                return checkpointB.CompareTo(checkpointA);
            }
            // Tertiary sort by distance to next checkpoint (ascending - less distance = better)
            return distanceA.CompareTo(distanceB);
        });

        for (int i = 0; i < sortedCars.Count; i++)
        {
            if (sortedCars[i] == car)
            {
                position = i + 1;
                break;
            }
        }

        return position;
    }

    string GetPositionText(int position)
    {
        return $"P{position}";
    }

    string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        int milliseconds = Mathf.FloorToInt((time * 1000f) % 1000f);

        return $"{minutes:00}:{seconds:00}.{milliseconds:000}";
    }

    void CheckRaceCompletion()
    {
        if (playerCar != null && carLaps.ContainsKey(playerCar.transform) && carLaps[playerCar.transform] >= totalLaps)
        {
            if (currentState != RaceState.Finished)
            {
                currentState = RaceState.Finished;
                Debug.Log("Player Finished! Race is now in Finished state.");
                // Don't show results immediately - let other cars finish first
                // Results will update dynamically as cars finish
                Invoke(nameof(ShowResults), 0.5f); // Small delay to ensure smooth transition
            }
        }
    }

    void HandleFinished()
    {
        // Continue updating UI even after player finishes
        UpdateUI();
    }

    void ShowResults()
    {
        if (inGameUIContainer != null) inGameUIContainer.SetActive(false);
        if (dimmingPanel != null) dimmingPanel.SetActive(true);
        if (resultsPanel != null) resultsPanel.SetActive(true);
        if (resultsTitleText != null) resultsTitleText.text = "Race Finished!";
        if (restartButton != null) restartButton.interactable = true;

        if (resultsContentParent != null)
        {
            foreach (Transform child in resultsContentParent)
            {
                Destroy(child.gameObject);
            }
        }

        Debug.Log("=== SHOWING RACE RESULTS ===");

        // Create a list of all cars with their finish positions or DNF status
        List<Transform> finishedCars = new List<Transform>();
        List<Transform> dnfCars = new List<Transform>();

        foreach (Transform car in allCars)
        {
            if (carFinishPosition.ContainsKey(car) && carFinishPosition[car] > 0)
            {
                finishedCars.Add(car);
            }
            else
            {
                dnfCars.Add(car);
            }
        }

        // Sort finished cars by finish position
        finishedCars.Sort((a, b) => carFinishPosition[a].CompareTo(carFinishPosition[b]));

        // Display finished cars
        foreach (Transform car in finishedCars)
        {
            string carName = car.name;
            float totalRaceTime = carTotalRaceTimes[car];
            int position = carFinishPosition[car];
            Debug.Log($"ShowResults: {carName} finished in position {position} with total time: {FormatTime(totalRaceTime)}");

            string resultEntry = $"{position}. {carName} - {FormatTime(totalRaceTime)}";

            if (resultEntryPrefab != null && resultsContentParent != null)
            {
                GameObject entry = Instantiate(resultEntryPrefab, resultsContentParent);
                Text entryText = entry.GetComponent<Text>();
                if (entryText != null)
                {
                    entryText.text = resultEntry;
                }
            }
        }

        // Display DNF cars
        foreach (Transform car in dnfCars)
        {
            string carName = car.name;
            string resultEntry = $"DNF. {carName} - Did Not Finish";
            Debug.Log($"ShowResults: {carName} is DNF.");

            if (resultEntryPrefab != null && resultsContentParent != null)
            {
                GameObject entry = Instantiate(resultEntryPrefab, resultsContentParent);
                Text entryText = entry.GetComponent<Text>();
                if (entryText != null)
                {
                    entryText.text = resultEntry;
                }
            }
        }
    }

    // CRITICAL FIX: Allow checkpoints to be processed even after player finishes
    public void CarPassedCheckpoint(Transform car, int checkpointNumber)
    {
        // Process checkpoints during Racing OR if the car hasn't finished yet
        if (currentState != RaceState.Racing && carFinishPosition.ContainsKey(car) && carFinishPosition[car] > 0)
        {
            // Car has already finished, ignore checkpoints
            return;
        }

        int expectedCheckpoint = (carCheckpoints[car] + 1) % GetTotalCheckpoints();

        if (checkpointNumber == expectedCheckpoint)
        {
            bool completingLap = (checkpointNumber == 0 && carCheckpoints[car] > 0);
            carCheckpoints[car] = checkpointNumber;

            if (completingLap)
            {
                CompleteLap(car);
            }
        }
    }

    void CompleteLap(Transform car)
    {
        carLaps[car]++;

        float lapTime = Time.time - carCurrentLapStartTimes[car];
        carLapTimes[car] = lapTime;
        carCurrentLapStartTimes[car] = Time.time;

        if (carLaps[car] >= totalLaps)
        {
            if (raceStartTime > 0f)
            {
                carTotalRaceTimes[car] = Time.time - raceStartTime;
                carFinishPosition[car] = nextFinishPosition; // Assign finish position
                nextFinishPosition++;

                Debug.Log($"{car.name} finished in position {carFinishPosition[car]} with time {FormatTime(carTotalRaceTimes[car])}");

                // If results panel is already showing, update it
                if (resultsPanel != null && resultsPanel.activeSelf)
                {
                    ShowResults();
                }
            }
        }

        // Check for power-up respawn
        if (car == playerCar) // Only respawn power-ups based on player's lap progress
        {
            List<GameObject> powerUpsToRespawn = new List<GameObject>();
            foreach (var entry in collectedPowerUps)
            {
                GameObject powerUpGameObject = entry.Key;
                int collectedLap = entry.Value;

                // Respawn if current lap is greater than collected lap + respawn laps
                if (carLaps[car] > collectedLap && (carLaps[car] - collectedLap) >= powerUpRespawnLaps)
                {
                    powerUpsToRespawn.Add(powerUpGameObject);
                }
            }

            foreach (GameObject pu in powerUpsToRespawn)
            {
                pu.SetActive(true);
                collectedPowerUps.Remove(pu); // Remove from collected list
                Debug.Log($"PowerUp {pu.name} respawned on lap {carLaps[car]}.");
            }
        }

        Debug.Log($"Lap {carLaps[car]} completed! Time: {FormatTime(lapTime)} for {car.name}. Total Laps: {carLaps[car]}/{totalLaps}");
    }

    int GetTotalCheckpoints()
    {
        return checkpoints.Count > 0 ? checkpoints.Count : 3;
    }

    public bool IsRaceActive()
    {
        return currentState == RaceState.Racing;
    }

    public int GetPlayerLap()
    {
        if (playerCar != null && carLaps.ContainsKey(playerCar.transform))
        {
            return carLaps[playerCar.transform];
        }
        return 0;
    }
}

