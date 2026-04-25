using Firebase;
using Firebase.Database;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public class LeaderbordService : MonoBehaviour
{
    public int topLimit = 3;
    private DatabaseReference _db;

    public event Action<List<LeaderboardEntry>> OnLeaderboardEntry;

    private void OnEnable()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                _db = FirebaseDatabase.DefaultInstance.RootReference;
                _ = LoadTopAsync();
            }
        });
    }

    public async Task LoadTopAsync()
    {
        var query = _db.Child("leaderboards").Child("global")
                       .OrderByChild("score")
                       .LimitToLast(topLimit);

        DataSnapshot snapshot = await query.GetValueAsync();

        List<LeaderboardEntry> result = new List<LeaderboardEntry>();

        if (snapshot.Exists)
        {
            foreach (var child in snapshot.Children)
            {
                var entry = JsonUtility.FromJson<LeaderboardEntry>(child.GetRawJsonValue());
                result.Add(entry);
            }
            result.Sort((a, b) => b.score.CompareTo(a.score));
        }
        OnLeaderboardEntry?.Invoke(result);
    }
}

[Serializable]
public class LeaderboardEntry
{
    public string uid;
    public string name;
    public long score;
}