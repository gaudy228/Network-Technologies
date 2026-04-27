using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class LeaderbordService : MonoBehaviour
{
    public int topLimit = 3;

    private const string DATABASE_URL = "https://telegram-mini-app-a32f1-default-rtdb.europe-west1.firebasedatabase.app/";

    public event Action<List<LeaderboardEntry>> OnLeaderboardLoaded;

    private void OnEnable()
    {
        LoadTopScores();
    }

    public void LoadTopScores()
    {
        StartCoroutine(LoadTopScoresCoroutine());
    }

    private IEnumerator LoadTopScoresCoroutine()
    {
        string url = $"{DATABASE_URL}leaderboards/global.json?orderBy=\"score\"&limitToLast={topLimit}";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            List<LeaderboardEntry> result = new List<LeaderboardEntry>();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;

                if (!string.IsNullOrEmpty(jsonResponse) && jsonResponse != "null")
                {
                    result = ParseLeaderboardData(jsonResponse);

                    result.Sort((a, b) => b.score.CompareTo(a.score));

                    Debug.Log($"Loaded {result.Count} leaderboard entries");
                }
                else
                {
                    Debug.Log("Leaderboard is empty");
                }
            }
            else
            {
                Debug.LogError($"Failed to load leaderboard: {request.error}");
                Debug.LogError($"Response: {request.downloadHandler.text}");
            }

            OnLeaderboardLoaded?.Invoke(result);
        }
    }

    private List<LeaderboardEntry> ParseLeaderboardData(string jsonResponse)
    {
        List<LeaderboardEntry> entries = new List<LeaderboardEntry>();

        try
        {
            jsonResponse = jsonResponse.Trim();
            if (jsonResponse.StartsWith("{") && jsonResponse.EndsWith("}"))
            {
                jsonResponse = jsonResponse.Substring(1, jsonResponse.Length - 2);
            }

            int braceLevel = 0;
            int lastIndex = 0;
            bool inString = false;

            for (int i = 0; i < jsonResponse.Length; i++)
            {
                char c = jsonResponse[i];

                if (c == '"' && (i == 0 || jsonResponse[i - 1] != '\\'))
                    inString = !inString;

                if (!inString)
                {
                    if (c == '{') braceLevel++;
                    if (c == '}') braceLevel--;

                    if (c == ',' && braceLevel == 0)
                    {
                        string entryStr = jsonResponse.Substring(lastIndex, i - lastIndex);
                        ParseSingleEntry(entryStr, entries);
                        lastIndex = i + 1;
                    }
                }
            }

            if (lastIndex < jsonResponse.Length)
            {
                string entryStr = jsonResponse.Substring(lastIndex);
                ParseSingleEntry(entryStr, entries);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error parsing leaderboard: {e.Message}");
            entries = AlternativeParseLeaderboardData(jsonResponse);
        }

        return entries;
    }

    private void ParseSingleEntry(string entryStr, List<LeaderboardEntry> entries)
    {
        try
        {
            int colonIndex = entryStr.IndexOf(':');
            if (colonIndex > 0)
            {
                string uid = entryStr.Substring(0, colonIndex).Trim().Trim('"');
                string dataJson = entryStr.Substring(colonIndex + 1).Trim();

                var entry = JsonUtility.FromJson<LeaderboardEntry>(dataJson);
                if (entry != null)
                {
                    entry.uid = uid;
                    entries.Add(entry);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Failed to parse entry: {e.Message}");
        }
    }

    private List<LeaderboardEntry> AlternativeParseLeaderboardData(string jsonResponse)
    {
        List<LeaderboardEntry> entries = new List<LeaderboardEntry>();

        try
        {
            var data = SimpleJSON(jsonResponse);
            if (data != null)
            {
                foreach (var kvp in data)
                {
                    var entry = new LeaderboardEntry
                    {
                        uid = kvp.Key,
                        name = GetValueFromJson(kvp.Value, "name"),
                        score = long.Parse(GetValueFromJson(kvp.Value, "score"))
                    };
                    entries.Add(entry);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Alternative parsing failed: {e.Message}");
        }

        return entries;
    }

    private Dictionary<string, string> SimpleJSON(string json)
    {
        var result = new Dictionary<string, string>();
        return result;
    }

    private string GetValueFromJson(string json, string key)
    {
        string searchKey = $"\"{key}\":";
        int startIndex = json.IndexOf(searchKey);
        if (startIndex == -1) return "";

        startIndex += searchKey.Length;
        int endIndex = startIndex;

        if (json[startIndex] == '"')
        {
            startIndex++;
            endIndex = json.IndexOf('"', startIndex);
            return json.Substring(startIndex, endIndex - startIndex);
        }
        else
        {
            while (endIndex < json.Length && (char.IsDigit(json[endIndex]) || json[endIndex] == '-'))
                endIndex++;
            return json.Substring(startIndex, endIndex - startIndex);
        }
    }

    public IEnumerator SubmitScore(string userId, string playerName, long score, string idToken)
    {
        string url = $"{DATABASE_URL}leaderboards/global/{userId}.json?auth={idToken}";

        string jsonData = $"{{\"name\":\"{playerName}\",\"score\":{score}}}";

        using (UnityWebRequest request = new UnityWebRequest(url, "PUT"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"Score {score} submitted for {playerName}");
                LoadTopScores();
            }
            else
            {
                Debug.LogError($"Failed to submit score: {request.error}");
                Debug.LogError($"Response: {request.downloadHandler.text}");
            }
        }
    }
}

[Serializable]
public class LeaderboardEntry
{
    public string uid;
    public string name;
    public long score;
}