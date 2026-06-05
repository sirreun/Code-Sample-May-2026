using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TimeManager : NetworkBehaviour
{
    public static TimeManager instance { get; private set; }
    private int totalTicks = 0;
    [Range(0,23)]
    public int startingHours = 13;
    [Range(0,59)]
    public int startingMinutes = 30;
    public TFTime startingTime = new TFTime(13, 30);
    public TFTime CurrentTime = new TFTime();
    public NetworkVariable<TimeInt> CurrentTime_SERVER;
    public struct TimeInt : INetworkSerializable
    {
        public int Hours;
        public int Minutes;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Hours);
            serializer.SerializeValue(ref Minutes);
        }
    }

    [Header("Ticks")]
    [Range(1,10)]
    public int ticksPerMinute = 1; // for debugging set to 1
    [Range(0,10)]
    public float secondsPerTick = 2f;

    private int ticksPerDay;
    private bool tickIsWaiting = false;
    public event Action Tick; // Set up in Start() for those initing at load time

    #region /// Initialzing ///
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Debug.LogWarning("Found more than one TimeManager, destroying the new one.");
            Destroy(this);
            return;
        }
        

        CurrentTime_SERVER = new NetworkVariable<TimeInt>(new TimeInt
            {
                Hours = startingTime.Hours,
                Minutes = startingTime.Minutes
            }, 
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);
    }
    public override void OnDestroy()
    {
        instance = null;
        base.OnDestroy();
    }

    private void Start()
    {
        startingTime.Hours = startingHours;
        startingTime.Minutes = startingMinutes;

        CurrentTime.Hours = startingTime.Hours;
        CurrentTime.Minutes = startingTime.Minutes;
    }

    public void InitializeMissionTime()
    {
        CurrentTime.Hours = startingTime.Hours;
        CurrentTime.Minutes = startingTime.Minutes;

        if (IsHost)
        {
            CurrentTime_SERVER.Value = new TimeInt
            {
                Hours = CurrentTime.Hours,
                Minutes = CurrentTime.Minutes
            };
        }
        else
        {
            CurrentTime.Hours = CurrentTime_SERVER.Value.Hours;
            CurrentTime.Minutes = CurrentTime_SERVER.Value.Minutes;
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsHost)
        {
            CurrentTime_SERVER.Value = new TimeInt
                {
                    Hours = CurrentTime.Hours,
                    Minutes = CurrentTime.Minutes
                };
        }
        else
        {
            CurrentTime.Hours = CurrentTime_SERVER.Value.Hours;
            CurrentTime.Minutes = CurrentTime_SERVER.Value.Minutes;
        }
        
    }
    #endregion

    public override void OnNetworkDespawn()
    {

    }

    void Update()
    {
        if (!tickIsWaiting)
        {
            StartCoroutine(TimeForTick());
            CurrentTicksToTwentyFourHourTime();
        }
    }

    #region /// Tick Functionallity ///
    private IEnumerator TimeForTick()
    {
        tickIsWaiting = true;
        
        yield return new WaitForSeconds(secondsPerTick);
        
        Tick?.Invoke();
        tickIsWaiting = false;
        totalTicks++;
    }

    /// <summary>
    /// Turns ticks into the current 24-hour time.
    /// </summary>
    /// <param name="ticks"></param>
    /// <returns></returns>
    private void CurrentTicksToTwentyFourHourTime()
    {
        if (!IsHost && IsSpawned)
        {
            CurrentTime.Hours = CurrentTime_SERVER.Value.Hours;
            CurrentTime.Minutes = CurrentTime_SERVER.Value.Minutes;
            return;
        }

        int minutesDelta = totalTicks / ticksPerMinute;
        //Debug.Log("Minutes since start: " + minutesDelta);
        int minutes = minutesDelta + startingTime.Minutes + (startingTime.Hours * 60);
        //Debug.Log("Time in minutes: " + minutes);
        int hours = (minutes / 60) % 24;
        minutes = minutes % 60;

        CurrentTime.Hours = hours;
        CurrentTime.Minutes = minutes;

        if (IsSpawned)
        {
            CurrentTime_SERVER.Value = new TimeInt
            {
                Hours = CurrentTime.Hours,
                Minutes = CurrentTime.Minutes
            };
        }

        //Debug.Log(CurrentTime.ToString());
    }

    private int TFTimeToTicks(TFTime time)
    {
        int output = 0;

        return output;
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

    public int ToMinutes()
    {
        return (Hours * 60) + Minutes;
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
