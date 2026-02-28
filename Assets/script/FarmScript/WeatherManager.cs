using UnityEngine;
using System.Collections;

public class WeatherManager : MonoBehaviour
{
    public static WeatherManager Instance;

    // 날씨 종류 확장 (예시)
    public enum WeatherType { Sunny, Rainy, Cloudy, Winter, Storm }
    public WeatherType currentWeather;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
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
            // 전체 날씨 개수 중에서 랜덤 선택
            currentWeather = (WeatherType)Random.Range(0, System.Enum.GetValues(typeof(WeatherType)).Length);
            Debug.Log($"현재 날씨: {currentWeather}");
        }
    }

    // 날씨에 따른 이동 속도 배율 반환 함수
    public float GetSpeedModifier()
    {
        switch (currentWeather)
        {
            case WeatherType.Winter: return 0.7f;  // 겨울: 30% 감소
            case WeatherType.Rainy: return 0.85f; // 비: 15% 감소
            case WeatherType.Sunny: return 1.2f;  // 맑음: 20% 증가
            default: return 1.0f;                  // 기본
        }
    }
}