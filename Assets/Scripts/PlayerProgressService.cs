using Firebase;
using Firebase.Auth;
using Firebase.Database;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class PlayerProgressService : MonoBehaviour
{
    private string databaseUrl = "https://telegram-mini-app-a32f1-default-rtdb.europe-west1.firebasedatabase.app/";

    public FirebaseUser CurrentUser { get; private set; }
    public bool IsReady { get; private set; }
    public long Counter { get; private set; }

    public Action<FirebaseUser> OnUserChanger;
    public Action<long> OnCounterChanger;
    public Action<string> OnStatus;

    private SynchronizationContext _unityContext;
    private FirebaseAuth _auth;
    private DatabaseReference _dbRoot;

    private bool _dirty = false;
    private bool _isSaving = false;

    public float _autosaveIntervalSeconds = 5f;

    private float _autosaveTimer = 0f;

    private void Awake()
    {
        _unityContext = SynchronizationContext.Current;
    }
    private void RunOnUnityThread(Action action)
    {
        if (_unityContext == null)
        {
            action();
            return;
        }
        _unityContext.Post(state => action(), null);
    }
    private void Start()
    {
        InitFirebase();
        if (_auth != null)
        {
            _ = LoadConterAsync();
        }
    }
    private void Update()
    {
        if (!IsReady || CurrentUser == null)
        {
            return;
        }
        _autosaveTimer += Time.unscaledDeltaTime;
        if (_autosaveTimer >= _autosaveIntervalSeconds)
        {
            _autosaveTimer = 0f;
            if (_dirty)
            {
                _ = SaveCounterAsync();
            }
        }
    }
    private void InitFirebase()
    {
        OnStatus?.Invoke("Initializing");
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            var dep = task.Result;
            if (dep != DependencyStatus.Available)
            {
                RunOnUnityThread(() => OnStatus?.Invoke(dep.ToString()));
                return;
            }
            RunOnUnityThread(() =>
            {
                var app = FirebaseApp.DefaultInstance;
                app.Options.DatabaseUrl = new Uri(databaseUrl);

                _auth = FirebaseAuth.DefaultInstance;
                _dbRoot = FirebaseDatabase.DefaultInstance.RootReference;

                _auth.StateChanged += HandleAuthStateChange;
                HandleAuthStateChange(this, null);
                IsReady = true;
                OnStatus?.Invoke("Ready");
            });
        });
    }
    private void HandleAuthStateChange(object sender, EventArgs e)
    {
        var newUser = _auth.CurrentUser;
        if (newUser == CurrentUser)
        {
            return;
        }
        CurrentUser = newUser;
        OnUserChanger?.Invoke(CurrentUser);
        if (CurrentUser != null)
        {
            OnStatus?.Invoke(CurrentUser.UserId);
            _ = LoadConterAsync();
        }
        else
        {
            Counter = 0;
            _dirty = false;
            OnCounterChanger?.Invoke(Counter);
            OnStatus?.Invoke("No user");
        }
    }
    public async Task LoadConterAsync()
    {
        if (CurrentUser == null)
        {
            OnStatus?.Invoke("No user");
            return;
        }
        string path = $"users/{CurrentUser.UserId}/counter";
        try
        {
            var snap = await _dbRoot.Child(path).GetValueAsync();
            long value = 0;
            if (snap != null && snap.Exists && long.TryParse(snap.Value.ToString(), out var parsed))
            {
                value = parsed;
                Counter = value;
                _dirty = false;
            }
            RunOnUnityThread(() => OnCounterChanger?.Invoke(Counter));
        }
        catch (Exception ex)
        {
            RunOnUnityThread(() => OnStatus?.Invoke(ex.ToString()));
        }
    }
    public void SetCounter(long value)
    {
        Counter = value;
        _dirty |= true;
        OnCounterChanger?.Invoke(Counter);
    }
    public void AddClicks(int amount = 1)
    {
        Counter += amount;
        _dirty = true;
        OnCounterChanger?.Invoke(Counter);
    }
    private async Task SaveCounterAsync()
    {
        if (CurrentUser == null)
        {
            return;
        }
        if (_isSaving)
        {
            return;
        }

        string uid = CurrentUser.UserId;

        var update = new Dictionary<string, object>
        {
            {$"users/{uid}/counter", Counter },
            {$"users/{uid}/updateAtUnix", DateTimeOffset.UtcNow.ToUnixTimeSeconds()},

            {$"leaderboards/global/{uid}/score", Counter},
            {$"leaderboards/global/{uid}/name", CurrentUser.Email}
        };
        try
        {
            await _dbRoot.UpdateChildrenAsync(update);
            _dirty = false;
            RunOnUnityThread(() => OnStatus?.Invoke("Saved"));
        }
        catch (Exception ex)
        {
            RunOnUnityThread(() => OnStatus?.Invoke(ex.ToString()));
        }
        finally
        {
            _isSaving = false;
        }
    }
    private void OnApplicationQuit()
    {
        if (_dirty)
        {
            _ = SaveCounterAsync();
        }
    }

    private void OnDestroy()
    {
        if (_auth != null)
        {
            _auth.StateChanged -= HandleAuthStateChange;
        }
    }
}
