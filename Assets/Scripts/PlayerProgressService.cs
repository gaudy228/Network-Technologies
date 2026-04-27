using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerProgressService : MonoBehaviour
{
    private string databaseUrl = "https://telegram-mini-app-a32f1-default-rtdb.europe-west1.firebasedatabase.app/";

    private const string API_KEY = "AIzaSyD3cn1bz2JJR-8V_z9fxeUkc44CV30wD_U";

    public string CurrentUserId { get; private set; }
    public string CurrentIdToken { get; private set; }
    public bool IsReady { get; private set; }
    public long Counter { get; private set; }

    public Action<string> OnUserChanged;
    public Action<long> OnCounterChanged;
    public Action<string> OnStatus;

    private bool _dirty = false;
    private bool _isSaving = false;
    private float _autosaveIntervalSeconds = 5f;
    private float _autosaveTimer = 0f;

    private void Start()
    {
        IsReady = true;
        OnStatus?.Invoke("Ready");

        LoadSavedUser();
    }

    private void Update()
    {
        if (!IsReady || string.IsNullOrEmpty(CurrentUserId))
        {
            return;
        }

        _autosaveTimer += Time.unscaledDeltaTime;
        if (_autosaveTimer >= _autosaveIntervalSeconds)
        {
            _autosaveTimer = 0f;
            if (_dirty && !_isSaving)
            {
                StartCoroutine(SaveCounterCoroutine());
            }
        }
    }

    private void LoadSavedUser()
    {
        if (PlayerPrefs.HasKey("FirebaseUserId") && PlayerPrefs.HasKey("FirebaseToken"))
        {
            CurrentUserId = PlayerPrefs.GetString("FirebaseUserId");
            CurrentIdToken = PlayerPrefs.GetString("FirebaseToken");
            OnUserChanged?.Invoke(CurrentUserId);
            OnStatus?.Invoke($"Loaded user: {CurrentUserId.Substring(0, 6)}...");
            StartCoroutine(LoadCounterCoroutine());
        }
        else
        {
            OnStatus?.Invoke("No user logged in");
        }
    }

    public void SetUser(string userId, string idToken)
    {
        CurrentUserId = userId;
        CurrentIdToken = idToken;
        PlayerPrefs.SetString("FirebaseUserId", userId);
        PlayerPrefs.SetString("FirebaseToken", idToken);
        PlayerPrefs.Save();

        OnUserChanged?.Invoke(CurrentUserId);
        OnStatus?.Invoke($"User set: {userId.Substring(0, 6)}...");

        StartCoroutine(LoadCounterCoroutine());
    }

    public void ClearUser()
    {
        CurrentUserId = null;
        CurrentIdToken = null;
        Counter = 0;
        _dirty = false;

        PlayerPrefs.DeleteKey("FirebaseUserId");
        PlayerPrefs.DeleteKey("FirebaseToken");
        PlayerPrefs.Save();

        OnUserChanged?.Invoke(null);
        OnCounterChanged?.Invoke(Counter);
        OnStatus?.Invoke("User cleared");
    }

    public void LoadCounter()
    {
        if (string.IsNullOrEmpty(CurrentUserId))
        {
            OnStatus?.Invoke("No user logged in");
            return;
        }
        StartCoroutine(LoadCounterCoroutine());
    }

    private IEnumerator LoadCounterCoroutine()
    {
        if (string.IsNullOrEmpty(CurrentUserId))
        {
            OnStatus?.Invoke("No user");
            yield break;
        }

        string path = $"users/{CurrentUserId}/counter.json?auth={CurrentIdToken}";
        string url = $"{databaseUrl}{path}";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string response = request.downloadHandler.text;

                if (!string.IsNullOrEmpty(response) && response != "null")
                {
                    if (long.TryParse(response.Trim('"'), out long value))
                    {
                        Counter = value;
                        _dirty = false;
                        OnCounterChanged?.Invoke(Counter);
                        OnStatus?.Invoke($"Counter loaded: {Counter}");
                    }
                    else
                    {
                        Counter = 0;
                        _dirty = true;
                        OnCounterChanged?.Invoke(Counter);
                        OnStatus?.Invoke("Counter initialized to 0");
                        StartCoroutine(SaveCounterCoroutine());
                    }
                }
                else
                {
                    Counter = 0;
                    _dirty = true;
                    OnCounterChanged?.Invoke(Counter);
                    OnStatus?.Invoke("Counter initialized to 0");
                    StartCoroutine(SaveCounterCoroutine());
                }
            }
            else
            {
                Debug.LogError($"Failed to load counter: {request.error}");
                OnStatus?.Invoke($"Load failed: {request.error}");
            }
        }
    }

    public void SetCounter(long value)
    {
        Counter = value;
        _dirty = true;
        OnCounterChanged?.Invoke(Counter);
    }

    public void AddClicks(int amount = 1)
    {
        Counter += amount;
        _dirty = true;
        OnCounterChanged?.Invoke(Counter);

        if (!_isSaving)
        {
            StartCoroutine(SaveCounterCoroutine());
        }
    }

    public void SaveCounter()
    {
        if (string.IsNullOrEmpty(CurrentUserId) || _isSaving)
        {
            return;
        }
        StartCoroutine(SaveCounterCoroutine());
    }

    private IEnumerator SaveCounterCoroutine()
    {
        if (string.IsNullOrEmpty(CurrentUserId) || _isSaving)
        {
            yield break;
        }

        _isSaving = true;
        string uid = CurrentUserId;
        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        string counterUrl = $"{databaseUrl}users/{uid}/counter.json?auth={CurrentIdToken}";
        string scoreUrl = $"{databaseUrl}leaderboards/global/{uid}/score.json?auth={CurrentIdToken}";
        string timeUrl = $"{databaseUrl}users/{uid}/updateAtUnix.json?auth={CurrentIdToken}";

        using (UnityWebRequest request = new UnityWebRequest(counterUrl, "PUT"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(Counter.ToString());
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Failed to save counter: {request.error}");
                OnStatus?.Invoke($"Save failed: {request.error}");
                _isSaving = false;
                yield break;
            }
        }

        using (UnityWebRequest request = new UnityWebRequest(timeUrl, "PUT"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(timestamp.ToString());
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();
        }

        using (UnityWebRequest request = new UnityWebRequest(scoreUrl, "PUT"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(Counter.ToString());
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();
        }

        _dirty = false;
        _isSaving = false;
        OnStatus?.Invoke($"Saved: {Counter}");
        Debug.Log($"Counter saved: {Counter}");
    }

    public IEnumerator UpdateLeaderboardName(string playerName)
    {
        if (string.IsNullOrEmpty(CurrentUserId) || string.IsNullOrEmpty(CurrentIdToken))
        {
            yield break;
        }

        string url = $"{databaseUrl}leaderboards/global/{CurrentUserId}/name.json?auth={CurrentIdToken}";

        using (UnityWebRequest request = new UnityWebRequest(url, "PUT"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes($"\"{playerName}\"");
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"Leaderboard name updated: {playerName}");
            }
            else
            {
                Debug.LogError($"Failed to update name: {request.error}");
            }
        }
    }

    private void OnApplicationQuit()
    {
        if (_dirty && !_isSaving && !string.IsNullOrEmpty(CurrentUserId))
        {
            StartCoroutine(SaveCounterCoroutine());
        }
    }
}