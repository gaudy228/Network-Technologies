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
        leaderboardService.OnLeaderboardEntry += OnLeaderboardEntryReceived;
    }

    private void OnDestroy()
    {
        leaderboardService.OnLeaderboardEntry -= OnLeaderboardEntryReceived;
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

        int countToShow = Mathf.Min(entries.Count, 3);
        Debug.Log($"Создание {countToShow} записей из {entries.Count}");

        for (int i = 0; i < countToShow; i++)
        {
            GameObject entryGO = Instantiate(entryPrefab, spawnGameObject.transform);
            TextMeshProUGUI textName = entryGO.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI textScore = entryGO.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
            textName.text = entries[i].name;
            textScore.text = entries[i].score.ToString();
        }
    }
}