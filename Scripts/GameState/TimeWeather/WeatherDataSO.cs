using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WeatherData", menuName = "ScriptableObjects/WeatherDataSO")]
[Serializable]
public class WeatherDataSO : ScriptableObject
{ 
    public Weather _Type;
    public int Weight = 0;
    public bool HasTemperatureRequirement = false;
    public int TemperatureRequirement = 0;
    public WeatherDataSO[] PossibleNextWeathers;
}
