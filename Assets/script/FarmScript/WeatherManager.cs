using UnityEngine;
using System.Collections;

public class WeatherManager : MonoBehaviour
{
    public static WeatherManager Instance; //�̱��� ����

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
            yield return new WaitForSeconds(10f); //10�ʸ��� ���� ����
            int randomWeather = Random.Range(0, 3);
            currentWeather = (WeatherType)randomWeather;
            Debug.Log($"���� ����: {currentWeather}");
        }
    }
}
