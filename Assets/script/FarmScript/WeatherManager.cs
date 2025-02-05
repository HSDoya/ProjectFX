using UnityEngine;
using System.Collections;

public class WeatherManager : MonoBehaviour
{
    public static WeatherManager Instance; //½Ì±ÛÅæ ÆÐÅÏ

    public enum WeatherType { Sunny, Rainy, Cloudy }
    public WeatherType currentWeather;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }
    private void start()
    {
        StartCoroutine(ChangeWeather());
    }
    private IEnumerator ChangeWeather()
    {
        while (true)
        {
            yield return new WaitForSeconds(10f); //10ÃÊ¸¶´Ù ³¯¾¾ º¯°æ
            int randomWeather = Random.Range(0, 3);
            currentWeather = (WeatherType)randomWeather;
            Debug.Log($"ÇöÀç ³¯¾¾: {currentWeather}");
        }
    }
}
