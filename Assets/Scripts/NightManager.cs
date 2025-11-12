using UnityEngine;
using UnityEngine.Events;

public class NightManager : MonoBehaviour
{
    public static NightManager Instance { get; private set; }

    [Header("Night Counter")]
    [SerializeField] private int currentNight = 0; // Start bij nacht 0

    [Header("References")]
    public DayNightCycle dayNightCycle; // Sleep hier jouw DayNightCycle object naartoe

    [Header("Events")]
    public UnityEvent<int> OnNightStarted = new UnityEvent<int>();
    public UnityEvent<int> OnDayStarted = new UnityEvent<int>();

    private bool wasNight = false;

    public int CurrentNight => currentNight;
    public bool IsNight => dayNightCycle != null && dayNightCycle.IsNightTime();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (dayNightCycle != null)
        {
            wasNight = dayNightCycle.IsNightTime();
        }
    }

    private void Update()
    {
        if (dayNightCycle == null) return;

        bool isNightNow = dayNightCycle.IsNightTime();

        // Dag -> Nacht
        if (isNightNow && !wasNight)
        {
            currentNight++;
            Debug.Log($"Night started: {currentNight}");
            OnNightStarted.Invoke(currentNight);
        }
        // Nacht -> Dag
        else if (!isNightNow && wasNight)
        {
            Debug.Log($"Day started: {currentNight}");
            OnDayStarted.Invoke(currentNight);
        }

        wasNight = isNightNow;
    }

    /// <summary>
    /// Manually starts the next night by setting the time to nighttime
    /// </summary>
    public void StartNight()
    {
        if (dayNightCycle == null)
        {
            Debug.LogError("NightManager: Cannot start night - dayNightCycle reference is null!");
            return;
        }

        // Force nighttime (18:00 = 18f)
        dayNightCycle.timeOfDay = 18f;
        Debug.Log("NightManager: Manually set time to nighttime (18:00)");
    }
}
