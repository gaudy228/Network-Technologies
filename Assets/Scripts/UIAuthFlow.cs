using UnityEngine;

public class UIAuthFlow : MonoBehaviour
{
    [SerializeField] private GameObject _loginPanel;
    [SerializeField] private GameObject _gamePanel;
    [SerializeField] private PlayerProgressService _progress;

    private void Start()
    {
        if (_progress == null)
        {
            Debug.LogError("UIAuthFlow: no progress set");
            return;
        }

        _progress.OnUserChanged += HandleUserChanged;

        CheckInitialState();
    }

    private void CheckInitialState()
    {
        bool isLoggedIn = !string.IsNullOrEmpty(_progress.CurrentUserId);
        Debug.Log($"UIAuthFlow: Initial state - Logged in: {isLoggedIn}, User ID: {_progress.CurrentUserId}");
        HandleUserChanged(isLoggedIn ? _progress.CurrentUserId : null);
    }

    private void HandleUserChanged(string userId)
    {
        bool loggedIn = !string.IsNullOrEmpty(userId);

        Debug.Log($"UIAuthFlow: HandleUserChanged - Logged in: {loggedIn}");

        if (_loginPanel != null)
        {
            _loginPanel.SetActive(!loggedIn);
            Debug.Log($"Login panel active: {!loggedIn}");
        }

        if (_gamePanel != null)
        {
            _gamePanel.SetActive(loggedIn);
            Debug.Log($"Game panel active: {loggedIn}");
        }
    }

    public void RefreshUI()
    {
        CheckInitialState();
    }

    private void OnDestroy()
    {
        if (_progress != null)
        {
            _progress.OnUserChanged -= HandleUserChanged;
        }
    }
}