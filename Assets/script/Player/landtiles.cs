using UnityEngine;
using System.Collections;

public class landtiles : MonoBehaviour
{
    private Renderer tileRenderer;
    private int currentStage = 0; // 0: 갈색(땅) → 1: 씨앗 → 2: 새싹 → 3: 성장 → 4: 수확 가능
    private bool isWatered = false; // 성장 중인지 체크
    private Color originalColor; // 초기 색상

    void Start()
    {
        tileRenderer = GetComponent<Renderer>();
        originalColor = new Color(0.5f, 0.3f, 0.2f); // 갈색(흙)
        tileRenderer.material.color = originalColor; // 초기 색상 적용
    }

    public void PlantSeed()
    {
        if (currentStage == 0) //씨앗 심기 가능할 떄만 실행
        {
            currentStage = 1; //씨앗 단계
            tileRenderer.material.color = Color.yellow; // 노란색(씨앗)
            isWatered |= false; // 물을 아직 안 줌
            Debug.Log("씨앗이 심어졌습니다.");
        }
    }

    public void WaterTile()
    {
        if(currentStage == 1) // 씨앗이 심어진 상태라면 물을 줄 수 있음
        {
            isWatered = true;
            tileRenderer.material.color = Color.blue; // 파란색 (물 줌)
            Debug.Log("물을 줬습니다! 작물이 성장합니다.");
            StartCoroutine(GrowSeed());
        }
    }

    private IEnumerator GrowSeed()
    {
        while (currentStage < 4) // 최대 성장 단계(수확 가능)까지 반복
        {
            if (!isWatered) yield break; // 물을 주지 않았다면 중단

            float growthTime = GetGrowthTime();
            yield return new WaitForSeconds(growthTime); // 날씨에 따른 성장 속도 반영
            currentStage++;
            UpdateTileColor();
            Debug.Log($"성장 단계 증가: {currentStage}");
        }
    }
    private float GetGrowthTime()
    {
        switch (WeatherManager.Instance.currentWeather)
        {
            case WeatherManager.WeatherType.Sunny:
                return 8f; // 맑음 → 기본 성장 속도 (8초)
            case WeatherManager.WeatherType.Rainy:
                return 4f; // 비 → 성장 속도 2배 빠름 (4초)
            case WeatherManager.WeatherType.Cloudy:
                return 12f; // 흐림 → 성장 속도 느림 (12초)
            default:
                return 8f;
        };
    }
    private void UpdateTileColor()
    {
        switch (currentStage)
        {
            case 1:
                tileRenderer.material.color = Color.yellow; // 씨앗
                break;
            case 2:
                tileRenderer.material.color = Color.green; // 새싹
                break;
            case 3:
                tileRenderer.material.color = new Color(0.1f, 0.6f, 0.1f); // 성장한 작물
                break;
            case 4:
                tileRenderer.material.color = new Color(0.8f, 0.5f, 0.1f); // 수확 가능 (갈색-노란색)
                break;
        }
    }
    public void HarvestCrop()
    {
        if (currentStage == 4) // 수확 가능 상태
        {
            currentStage = 0; // 다시 갈색 땅으로 초기화
            tileRenderer.material.color = originalColor;
            isWatered = false;
            Debug.Log("작물이 수확되었습니다!");
        }
    }
    //제거해도 되는 코드?
        public void ChangeTileColor(Color newColor, float duration)
        {
            StopAllCoroutines(); // 기존 코루틴 중지
            StartCoroutine(ChangeColorRoutine(newColor, duration));
        }
        private IEnumerator ChangeColorRoutine(Color newColor, float duration)
        {
            tileRenderer.material.color = newColor; // 새 색상 적용
            yield return new WaitForSeconds(duration); // 지정된 시간 대기
            tileRenderer.material.color = originalColor; // 원래 색상으로 복구
        }
}






