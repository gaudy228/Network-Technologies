using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LeaderboardUI : MonoBehaviour
{
    [SerializeField] private LeaderbordService leaderboardService;
    [SerializeField] private GameObject entryPrefab;

    private void Awake()
    {
        leaderboardService.OnLeaderboardEntry += UpdateLeaderboardUI;
    }

    private void OnDestroy()
    {
        leaderboardService.OnLeaderboardEntry -= UpdateLeaderboardUI;
    }

    private void UpdateLeaderboardUI(List<LeaderboardEntry> entries)
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        for (int i = 0; i < 3; i++)
        {
            GameObject entryGO = Instantiate(entryPrefab, transform);
            TextMeshProUGUI textName = entryGO.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI textScore = entryGO.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
            textName.text = $"{entries[i].name}";
            textScore.text = $"{entries[i].score}";
        }
    }
}
