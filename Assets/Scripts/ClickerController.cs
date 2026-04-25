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
        _progress.OnCounterChanger += UpdateUI;
        _progress.OnUserChanger += UserChange;
    }
    public void OnClickPressed()
    {
        if (_progress?.CurrentUser == null)
        {
            return;
        }
        _progress.AddClicks();
    }
    private void UpdateUI(long value)
    {
        if (_counter.text != null)
        {
            _counter.text = $"{value}";
        }
    }
    private void UserChange(Firebase.Auth.FirebaseUser user)
    {
        if (_gamePanel != null)
        {
            _gamePanel.SetActive(user != null);
        }
    }
    private void OnDestroy()
    {
        _progress.OnCounterChanger -= UpdateUI;
        _progress.OnUserChanger -= UserChange;
    }
}
