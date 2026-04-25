using Firebase.Auth;
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
        _progress.OnUserChanger += HandleUserChanged;
        HandleUserChanged(_progress.CurrentUser);
    }

    private void HandleUserChanged(FirebaseUser user)
    {
        bool loggenIn = (user != null);
        if (_loginPanel != null)
        {
            _loginPanel.SetActive(!loggenIn);
        }
        if (_gamePanel != null)
        {
            _gamePanel.SetActive(loggenIn);
        }
    }
    private void OnDestroy()
    {
        _progress.OnUserChanger -= HandleUserChanged;
    }
}
