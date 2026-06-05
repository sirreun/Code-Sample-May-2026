using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static TimeManager;

public class WeatherManager : NetworkBehaviour
{
    public static WeatherManager instance {  get; private set; }

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
    [SerializeField] private Biome biome;
    private Dictionary<Biome, WeatherDataSO[]> biomeToWeatherWeightDictionary = new Dictionary<Biome, WeatherDataSO[]>();
    [SerializeField] private WeatherDataSO[] weatherWeight;
    private Queue<WeatherDataSO> weatherForecast = new Queue<WeatherDataSO>();
    private int chanceToChangeWeather = 0;
    [SerializeField] private int minTicksToChangeWeather = 3;
    private int numberOfTicksSinceLastChangeWeather = 0;

    [Header("Weather Systems")]
    [SerializeField] private GameObject RainParticleObject;
    private ParticleSystem rainParticles;

    public enum Biome
    {
        ExampleBiome = 0
    }

    private void Awake()
    {
        instance = this;

        CurrentWeather_SERVER = new NetworkVariable<int>(0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        LoadBiomesFromResources();
        SetWeatherWeightsFromBiome();
    }

    // Start is called before the first frame update
    void Start()
    {
        MissionManager.instance.MissionStart += SubscribeToTick;
        MissionManager.instance.MissionEnd += UnsubscribeToTick;

        MissionManager.instance.MissionStart += InitializeMissionWeather;
    }

    public void InitializeMissionWeather()
    {
        CurrentWeather = RandomFromWeights(weatherWeight);
        RecentlyQueuedWeather = CurrentWeather;

        for (int i = 0; i < weatherForecastLength; i++)
        {
            RecentlyQueuedWeather = RandomFromWeights(RecentlyQueuedWeather.PossibleNextWeathers);
            weatherForecast.Enqueue(RecentlyQueuedWeather);
        }

        float ticksPerDay = TimeManager.instance.ticksPerMinute * 60 * 24;
        sunRotationSpeed = 360f / (ticksPerDay * TimeManager.instance.secondsPerTick);
        sunTransform.localEulerAngles = GetSunRotationFromTime(TimeManager.instance.startingTime);

        rainParticles = RainParticleObject.GetComponent<ParticleSystem>();
        var em = rainParticles.emission;
        em.enabled = false; // TODO: is causing issues with clients not loading in with rain if current weather is rain
    }

    public override void OnNetworkSpawn()
    {
        if (!IsHost)
        {
            CurrentWeather_SERVER.OnValueChanged += OnServerChangedWeather_CLIENT;
        }
    }


    public override void OnDestroy()
    {
        if (TimeManager.instance != null)
        {
            TimeManager.instance.Tick -= UpdateWeather;
        }
        

        MissionManager.instance.MissionStart -= SubscribeToTick;
        MissionManager.instance.MissionEnd -= UnsubscribeToTick;
        MissionManager.instance.MissionStart -= InitializeMissionWeather;

        instance = null;
        base.OnDestroy();
    }

    public override void OnNetworkDespawn()
    {
        CurrentWeather_SERVER.OnValueChanged -= OnServerChangedWeather_CLIENT;
    }

    // Update is called once per frame
    void Update()
    {
        UpdateDayNightCycle();
    }

    #region /// BIOMES ///

    public void ChangeBiome(Biome newBiome)
    {
        biome = newBiome;
        SetWeatherWeightsFromBiome();
    }

    private void SetWeatherWeightsFromBiome()
    {
        weatherWeight = biomeToWeatherWeightDictionary[biome];
    }

    private void LoadBiomesFromResources()
    {
        foreach (Biome biomeName in Enum.GetValues(typeof(Biome)))
        {
            WeatherDataSO[] biomeWeather = Resources.LoadAll<WeatherDataSO>("Weather/" + biomeName.ToString());
            biomeToWeatherWeightDictionary.Add(biomeName, biomeWeather);
        }
    }

    #endregion

    #region /// WEATHER FUNCTIONALLITY ///
    public void SubscribeToTick()
    {
        TimeManager.instance.Tick += UpdateWeather;
    }

    public void UnsubscribeToTick()
    {
        TimeManager.instance.Tick -= UpdateWeather;
    }

    /// <summary>
    /// Called every tick to check if weather will be changed.
    /// </summary>
    private void UpdateWeather()
    {
        if (!IsHost) return;

        numberOfTicksSinceLastChangeWeather++;
        if (numberOfTicksSinceLastChangeWeather >= minTicksToChangeWeather)
        {
            chanceToChangeWeather += 5; // NOTE: currently a linear chance to change weather, up by 5 each tick
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

    public void OnServerChangedWeather_CLIENT(int previousWeatherIndex, int newWeatherIndex)
    {
        if (IsHost) return;

        CurrentWeather = weatherWeight[newWeatherIndex];
        if (previousWeatherIndex >= 0 && previousWeatherIndex < weatherWeight.Length)
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
            PreviousWeather = previousWeather._Type;
        }

        switch (PreviousWeather)
        {
            case Weather.Sunny:
                break;
            case Weather.Overcast:
                break;
            case Weather.Windy:
                break;
            case Weather.Rainy:
                SetRain(false);
                break;
            case Weather.Snowy:
                break;
            case Weather.Thunderstorm:
                break;
            case Weather.Windstorm:
                break;
            case Weather.Snowstorm:
                break;

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
                Debug.LogWarning("Unable to update server weather due to error finding index");
            }
        }

        switch (CurrentWeather._Type)
        {
            case Weather.Sunny:
                break;
            case Weather.Overcast:
                break;
            case Weather.Windy:
                break;
            case Weather.Rainy:
                SetRain(true);
                break;
            case Weather.Snowy:
                break;
            case Weather.Thunderstorm:
                break;
            case Weather.Windstorm:
                break;
            case Weather.Snowstorm:
                break;

        }

        //Debug.Log("== New Weather: " + CurrentWeather + " ==");
    }

    private void ChangeWeather_SERVER()
    {
        numberOfTicksSinceLastChangeWeather = 0;
        chanceToChangeWeather = 0;

        // Decide next weather in forecast
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
        for (int i = 0; i < weatherWeight.Length; i++)
        {
            if (weatherWeight[i] == weather)
            {
                weatherFound = true;
                return i;
            }
        }

        Debug.LogWarning("Weather not found in weather weight list");
        weatherFound = false;
        return -1;
    }

    private WeatherDataSO RandomFromWeights(WeatherDataSO[] weatherList)
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
        Debug.LogWarning("RandomFromWeights: Wasn't able to pick a random weather, returning null");
        return null;
    }
    #endregion

    #region /// DAY NIGHT CYCLE ///
    
    private void UpdateDayNightCycle()
    {
        // Rotate the sun
        sunTransform.Rotate(Vector3.right, sunRotationSpeed * Time.deltaTime);

        // Adjust ambient lighting
        float timeFactor = Mathf.InverseLerp(-90, 90, sunTransform.eulerAngles.x);
        RenderSettings.ambientLight = ambientLightGradient.Evaluate(timeFactor);
        DebugAmbientLight = ambientLightGradient.Evaluate(timeFactor);

        // Fade Skyboxes

    }

    private Vector3 GetSunRotationFromTime(TFTime time)
    {
        int timeInMinutes = time.ToMinutes();

        //Debug.Log("start time: " + time.ToString() + " to mins: " + timeInMinutes);

        float numerator = timeInMinutes - 360f;
        if (numerator < 0)
        {
            numerator = 1440 + numerator;
        }

        float percentRotation = (numerator * 360f) / 1440f;
        //Debug.Log("num:" + numerator + "sun rotation set to " + percentRotation);
        Vector3 output = new Vector3(percentRotation, 0f, 0f);

        return output;
    }

    #endregion
}
