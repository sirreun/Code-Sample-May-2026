using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TimeManager : NetworkBehaviour
{
    public static TimeManager instance { get; private set; }
    private int totalTicks = 0;
    private TFTime startingTime = new TFTime(13, 30);
    public TFTime CurrentTime = new TFTime();
    public NetworkVariable<(int, int)> CurrentTime_SERVER;

    [Header("Ticks")]
    [Range(1,10)]
    [SerializeField] private int ticksPerMinute = 1; // For debugging is set to 1
    [Range(0,10)]
    [SerializeField] private float secondsPerTick = 2f;

    private int ticksPerDay;
    private bool tickIsWaiting = false;
    public event Action Tick;

    [Header("Day Night Cycle")]
    [SerializeField] private Transform sunTransform;
    [SerializeField] private Light sunLight;
    [SerializeField] private Gradient ambientLightGradient;
    public Color DebugAmbientLight;
    private float sunRotationSpeed;

    [Header("Weather")]
    public NetworkVariable<int> CurrentWeather_SERVER;
    public WeatherDataSO CurrentWeather;
    private WeatherDataSO previousWeather;
    private WeatherDataSO RecentlyQueuedWeather;
    private int weatherForecastLength = 4;
    [SerializeField] private List<WeatherDataSO> weatherWeight;
    private Queue<WeatherDataSO> weatherForecast = new Queue<WeatherDataSO>();
    private int chanceToChangeWeather = 0;
    [SerializeField] private int minTicksToChangeWeather = 3;
    private int numberOfTicksSinceLastChangeWeather = 0;

    [Header("Weather Systems")]
    [SerializeField] private GameObject RainParticleObject;
    private ParticleSystem rainParticles;

    #region /// Initialzing ///
    private void Awake()
    {
        instance = this;

        CurrentTime_SERVER = new NetworkVariable<(int, int)>((startingTime.Hours, startingTime.Minutes), 
            NetworkVariableReadPermission.Everyone, 
            NetworkVariableWritePermission.Server);
    }

    public override void OnDestroy()
    {
        instance = null;
    }

    private void Start()
    {
        CurrentTime.Hours = startingTime.Hours;
        CurrentTime.Minutes = startingTime.Minutes;


        CurrentWeather = RandomFromWeights(weatherWeight);
        RecentlyQueuedWeather = CurrentWeather;

        for (int i = 0; i < weatherForecastLength; i++)
        {
            RecentlyQueuedWeather = RandomFromWeights(RecentlyQueuedWeather.PossibleNextWeathers);
            weatherForecast.Enqueue(RecentlyQueuedWeather);
        }

        ticksPerDay = ticksPerMinute * 60 * 24;
        sunRotationSpeed = 360f / (ticksPerDay * secondsPerTick);

        rainParticles = RainParticleObject.GetComponent<ParticleSystem>();
        var em = rainParticles.emission;
        em.enabled = false;
    }

    public override void OnNetworkSpawn()
    {
        if (IsHost)
        {
            CurrentTime_SERVER.Value = (CurrentTime.Hours, CurrentTime.Minutes);
        }
        else
        {
            CurrentTime.Hours = CurrentTime_SERVER.Value.Item1;
            CurrentTime.Minutes = CurrentTime_SERVER.Value.Item2;

            CurrentWeather_SERVER.OnValueChanged += OnServerChangedWeather_CLIENT;
        }
        
    }
    #endregion

    public override void OnNetworkDespawn()
    {
        CurrentWeather_SERVER.OnValueChanged -= OnServerChangedWeather_CLIENT;
    }

    void Update()
    {
        if (!tickIsWaiting)
        {
            StartCoroutine(TimeForTick());
            CurrentTicksToTwentyFourHourTime();
        }

        UpdateDayNightCycle();
    }

    #region /// Tick Functionality ///
    private IEnumerator TimeForTick()
    {
        tickIsWaiting = true;
        
        yield return new WaitForSeconds(secondsPerTick);
        
        Tick?.Invoke();
        tickIsWaiting = false;
        totalTicks++;
        UpdateWeather();
    }

    /// <summary>
    /// Turns ticks into the current 24-hour time.
    /// </summary>
    /// <param name="ticks"></param>
    /// <returns></returns>
    private void CurrentTicksToTwentyFourHourTime()
    {
        int minutesDelta = totalTicks / ticksPerMinute;
        //Debug.Log("Minutes since start: " + minutesDelta);
        int minutes = minutesDelta + startingTime.Minutes + (startingTime.Hours * 60);
        //Debug.Log("Time in minutes: " + minutes);
        int hours = (minutes / 60) % 24;
        minutes = minutes % 60;

        CurrentTime.Hours = hours;
        CurrentTime.Minutes = minutes;
        //Debug.Log(CurrentTime.ToString());
    }

    private int TFTimeToTicks(TFTime time)
    {
        int output = 0;

        return output;
    }
    #endregion

    #region /// Weather Functionality ///

    /// <summary>
    /// Called every tick to check if weather will be changed.
    /// </summary>
    private void UpdateWeather()
    {
        if (!IsHost) return;

        numberOfTicksSinceLastChangeWeather++;
        if (numberOfTicksSinceLastChangeWeather >= minTicksToChangeWeather)
        {
            chanceToChangeWeather += 5; // Currently a linear increase in chance to change weather, up by 5% each tick
        }

        //Debug.Log("Chance to change weather: " + chanceToChangeWeather);

        if (chanceToChangeWeather == 0)
        {
            return;
        }

        if (chanceToChangeWeather >= 100)
        {
            ChangeWeather_SERVER();
        }

        int randomValue = UnityEngine.Random.Range(0, 100);

        if (randomValue < chanceToChangeWeather)
        {
            ChangeWeather_SERVER();
        }
    }

    public void OnServerChangedWeather_CLIENT(int newWeatherIndex, int previousWeatherIndex)
    {
        if (IsHost) return;

        CurrentWeather = weatherWeight[newWeatherIndex];
        if (previousWeatherIndex >= 0 && previousWeatherIndex < weatherWeight.Count)
        {
            previousWeather = weatherWeight[previousWeatherIndex];
        }
        ChangeWeather();
    }

    private void ChangeWeather()
    {
        Weather PreviousWeather;
        if (IsHost)
        {
            PreviousWeather = CurrentWeather._Type;
        }
        else
        {
            if (previousWeather != null)
            {
                PreviousWeather = previousWeather._Type;
            }
            
        }

        if (previousWeather != null)
        {
            switch (PreviousWeather)
            {
                case Weather.Sunny:
                case Weather.Overcast:
                case Weather.Windy:
                case Weather.Rainy:
                    SetRain(false);
                    break;
                case Weather.Snowy:
                case Weather.Thunderstorm:
                case Weather.Windstorm:
                case Weather.Snowstorm:
                default:
                    break;

            }
        }

        

        if (IsHost)
        {
            CurrentWeather = weatherForecast.Dequeue();
            int weatherIndex = WeatherWeightsToIndex(CurrentWeather, out bool weatherFound);
            if (weatherFound)
            {
                CurrentWeather_SERVER.Value = weatherIndex;
            }
            else
            {
                Debug.LogWarning("Unable to update server weather due to error finding weather index");
            }
        }

        switch (CurrentWeather._Type)
        {
            case Weather.Sunny:
            case Weather.Overcast:
            case Weather.Windy:
            case Weather.Rainy:
                SetRain(true);
                break;
            case Weather.Snowy:
            case Weather.Thunderstorm:
            case Weather.Windstorm:
            case Weather.Snowstorm:
                break;

        }

        //Debug.Log("== New Weather: " + CurrentWeather + " ==");
    }

    private void ChangeWeather_SERVER()
    {
        numberOfTicksSinceLastChangeWeather = 0;
        chanceToChangeWeather = 0;


        RecentlyQueuedWeather = RandomFromWeights(RecentlyQueuedWeather.PossibleNextWeathers);
        weatherForecast.Enqueue(RecentlyQueuedWeather);

        ChangeWeather();
    }

    private void SetRain(bool set)
    {
        var em = rainParticles.emission;
        em.enabled = set;
    }

    private int WeatherWeightsToIndex(WeatherDataSO weather, out bool weatherFound)
    {
        for (int i = 0; i < weatherWeight.Count; i++)
        {
            if (weatherWeight[i] == weather)
            {
                weatherFound = true;
                return i;
            }
        }

        Debug.LogWarning("Weather not found in weather weight list.");
        weatherFound = false;   
        return -1;
    }

    private WeatherDataSO RandomFromWeights(List<WeatherDataSO> weatherList)
    {
        int totalWeight = 0;
        foreach (WeatherDataSO weather in weatherList)
        {
            totalWeight += weather.Weight;
        }

        int randomValue = UnityEngine.Random.Range(0, totalWeight);

        foreach (WeatherDataSO weather in weatherList)
        {
            if (randomValue < weather.Weight)
            {
                return weather;
            }

            randomValue = randomValue - weather.Weight;
        }
        Debug.LogWarning("RandomFromWeights: Wasn't able to pick a random weather due to weather weight values being incorrect, returning null");
        return null;
    }
    #endregion

    #region /// Day Night Cycle ///
    
    private void UpdateDayNightCycle()
    {
        // Rotate the sun
        sunTransform.Rotate(Vector3.right, sunRotationSpeed * Time.deltaTime);

        // Adjust ambient lighting
        float timeFactor = Mathf.InverseLerp(-90, 90, sunTransform.eulerAngles.x);
        RenderSettings.ambientLight = ambientLightGradient.Evaluate(timeFactor);
        DebugAmbientLight = ambientLightGradient.Evaluate(timeFactor);

    }
    #endregion
}

public class TFTime
{
    public int Hours;
    public int Minutes;

    public TFTime(string time)
    {
        string[] strings = time.Split(":");
        if (strings.Length != 2)
        {
            Debug.LogWarning("Given string for TFTime is incorrect format: " + time);
        }
        else
        {
            try
            {
                Int32.TryParse(strings[0], out int hours);
                Hours = hours;
                Int32.TryParse(strings[1], out int minutes);
                Minutes = minutes;
            }
            catch
            {
                Debug.LogWarning("Not given number values to TFTime: " + time);
            }
        }
    }

    public TFTime(int hours, int minutes)
    {
        Hours = hours;
        Minutes = minutes;
    }

    public TFTime() 
    {
        Hours = 0;
        Minutes = 0;
    }

    public override string ToString()
    {
        string hours = Hours.ToString();
        string minutes = Minutes.ToString();

        if (Hours < 10)
        {
            hours = "0" + hours;
        }

        if (Minutes < 10)
        {
            minutes = "0" + minutes;
        }

        return hours + ":" + minutes;
    }
}

public enum Weather
{
    Sunny,
    Overcast,
    Foggy,
    Windy,
    Rainy,
    Snowy,
    Thunderstorm,
    Windstorm,
    Snowstorm
}
