using UnityEngine;
using System.Collections;

public class WeatherManager : MonoBehaviour
{
    public static WeatherManager Instance;

    public enum WeatherType { Sunny, Rainy, Cloudy }
    public WeatherType currentWeather;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        StartCoroutine(ChangeWeather());
    }

    private IEnumerator ChangeWeather()
    {
        while (true)
        {
            yield return new WaitForSeconds(10f);
            currentWeather = (WeatherType)Random.Range(0, 3);
            Debug.Log($"ÇöÀç ³¯¾¾: {currentWeather}");
        }
    }
}
