using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LeaderboardUI : MonoBehaviour
{
    [SerializeField] private LeaderbordService leaderboardService;
    [SerializeField] private GameObject entryPrefab;
    [SerializeField] private GameObject spawnGameObject;

    private Queue<List<LeaderboardEntry>> _pendingUpdates = new Queue<List<LeaderboardEntry>>();

    private void Awake()
    {
        leaderboardService.OnLeaderboardLoaded += OnLeaderboardEntryReceived;
    }

    private void OnDestroy()
    {
        leaderboardService.OnLeaderboardLoaded -= OnLeaderboardEntryReceived;
    }

    private void OnLeaderboardEntryReceived(List<LeaderboardEntry> entries)
    {
        lock (_pendingUpdates)
        {
            _pendingUpdates.Enqueue(entries);
        }
    }

    private void Update()
    {
        lock (_pendingUpdates)
        {
            while (_pendingUpdates.Count > 0)
            {
                var entries = _pendingUpdates.Dequeue();
                UpdateLeaderboardUI(entries);
            }
        }
    }

    private void UpdateLeaderboardUI(List<LeaderboardEntry> entries)
    {
        foreach (Transform child in spawnGameObject.transform)
        {
            Destroy(child.gameObject);
        }

        if (entries == null || entries.Count == 0)
        {
            Debug.Log("Нет записей в лидерборде");
        }

        int countToShow = Mathf.Min(entries.Count, 3);
        Debug.Log($"Создание {countToShow} записей из {entries.Count}");

        for (int i = 0; i < countToShow; i++)
        {
            if (entries[i] == null)
            {
                Debug.LogWarning($"Запись {i} равна null");
                continue;
            }

            GameObject entryGO = Instantiate(entryPrefab, spawnGameObject.transform);

            TextMeshProUGUI textName = entryGO.transform.GetChild(0)?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI textScore = entryGO.transform.GetChild(1)?.GetComponent<TextMeshProUGUI>();

            if (textName != null)
                textName.text = entries[i].name ?? "Unknown";
            else
                Debug.LogError("TextName component not found at child 0");

            if (textScore != null)
                textScore.text = entries[i].score.ToString();
            else
                Debug.LogError("TextScore component not found at child 1");
        }
    }

    public void RefreshLeaderboard()
    {
        if (leaderboardService != null)
        {
            leaderboardService.LoadTopScores();
        }
        else
        {
            Debug.LogError("LeaderbordService не назначен в инспекторе!");
        }
    }
}