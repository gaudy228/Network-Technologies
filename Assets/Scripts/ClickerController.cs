using TMPro;
using UnityEngine;

public class ClickerController : MonoBehaviour
{
    [SerializeField] private PlayerProgressService _progress;
    [SerializeField] private TMP_Text _counter;
    [SerializeField] private GameObject _gamePanel;

    private void Start()
    {
        if (_progress == null)
        {
            Debug.LogError($"{nameof(_progress)} is null");
            return;
        }

        _progress.OnCounterChanged += UpdateUI;
        _progress.OnUserChanged += UserChange;

        UpdateUI(_progress.Counter);
        bool isLoggedIn = !string.IsNullOrEmpty(_progress.CurrentUserId);
        UserChange(isLoggedIn ? _progress.CurrentUserId : null);
    }

    public void OnClickPressed()
    {
        if (string.IsNullOrEmpty(_progress?.CurrentUserId))
        {
            Debug.Log("Cannot add clicks: User not logged in");
            return;
        }

        _progress.AddClicks();
    }

    private void UpdateUI(long value)
    {
        if (_counter != null)
        {
            _counter.text = $"{value}";
        }
        else
        {
            Debug.LogWarning("Counter text component is null");
        }
    }

    private void UserChange(string userId)
    {
        bool isLoggedIn = !string.IsNullOrEmpty(userId);

        if (_gamePanel != null)
        {
            _gamePanel.SetActive(isLoggedIn);
        }

        Debug.Log($"ClickerController: User {(isLoggedIn ? "logged in" : "logged out")}");

        if (!isLoggedIn && _counter != null)
        {
            _counter.text = "0";
        }
    }

    private void OnDestroy()
    {
        if (_progress != null)
        {
            _progress.OnCounterChanged -= UpdateUI;
            _progress.OnUserChanged -= UserChange;
        }
    }
}
