using UnityEngine;
using System.Collections;

public class landtiles : MonoBehaviour
{
    private Renderer tileRenderer;
    private int currentStage = 0; // 0: ����(��) �� 1: ���� �� 2: ���� �� 3: ���� �� 4: ��Ȯ ����
    private bool isWatered = false; // ���� ������ üũ
    private Color originalColor; // �ʱ� ����

    void Start()
    {
        tileRenderer = GetComponent<Renderer>();
        originalColor = new Color(0.5f, 0.3f, 0.2f); // ����(��)
        tileRenderer.material.color = originalColor; // �ʱ� ���� ����
    }

    public void PlantSeed()
    {
        if (currentStage == 0) //���� �ɱ� ������ ���� ����
        {
            currentStage = 1; //���� �ܰ�
            tileRenderer.material.color = Color.yellow; // �����(����)
            isWatered |= false; // ���� ���� �� ��
            Debug.Log("������ �ɾ������ϴ�.");
        }
    }

    public void WaterTile()
    {
        if(currentStage == 1) // ������ �ɾ��� ���¶�� ���� �� �� ����
        {
            isWatered = true;
            tileRenderer.material.color = Color.blue; // �Ķ��� (�� ��)
            Debug.Log("���� ����ϴ�! �۹��� �����մϴ�.");
            StartCoroutine(GrowSeed());
        }
    }

    private IEnumerator GrowSeed()
    {
        while (currentStage < 4) // �ִ� ���� �ܰ�(��Ȯ ����)���� �ݺ�
        {
            if (!isWatered) yield break; // ���� ���� �ʾҴٸ� �ߴ�

            float growthTime = GetGrowthTime();
            yield return new WaitForSeconds(growthTime); // ������ ���� ���� �ӵ� �ݿ�
            currentStage++;
            UpdateTileColor();
            Debug.Log($"���� �ܰ� ����: {currentStage}");
        }
    }
    private float GetGrowthTime()
    {
        switch (WeatherManager.Instance.currentWeather)
        {
            case WeatherManager.WeatherType.Sunny:
                return 8f; // ���� �� �⺻ ���� �ӵ� (8��)
            case WeatherManager.WeatherType.Rainy:
                return 4f; // �� �� ���� �ӵ� 2�� ���� (4��)
            case WeatherManager.WeatherType.Cloudy:
                return 12f; // �帲 �� ���� �ӵ� ���� (12��)
            default:
                return 8f;
        };
    }
    private void UpdateTileColor()
    {
        switch (currentStage)
        {
            case 1:
                tileRenderer.material.color = Color.yellow; // ����
                break;
            case 2:
                tileRenderer.material.color = Color.green; // ����
                break;
            case 3:
                tileRenderer.material.color = new Color(0.1f, 0.6f, 0.1f); // ������ �۹�
                break;
            case 4:
                tileRenderer.material.color = new Color(0.8f, 0.5f, 0.1f); // ��Ȯ ���� (����-�����)
                break;
        }
    }
    public void HarvestCrop()
    {
        if (currentStage == 4) // ��Ȯ ���� ����
        {
            currentStage = 0; // �ٽ� ���� ������ �ʱ�ȭ
            tileRenderer.material.color = originalColor;
            isWatered = false;
            Debug.Log("�۹��� ��Ȯ�Ǿ����ϴ�!");
        }
    }
    //�����ص� �Ǵ� �ڵ�?
        public void ChangeTileColor(Color newColor, float duration)
        {
            StopAllCoroutines(); // ���� �ڷ�ƾ ����
            StartCoroutine(ChangeColorRoutine(newColor, duration));
        }
        private IEnumerator ChangeColorRoutine(Color newColor, float duration)
        {
            tileRenderer.material.color = newColor; // �� ���� ����
            yield return new WaitForSeconds(duration); // ������ �ð� ���
            tileRenderer.material.color = originalColor; // ���� �������� ����
        }
}






