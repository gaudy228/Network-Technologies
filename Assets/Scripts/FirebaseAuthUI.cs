using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class FirebaseAuthUI : MonoBehaviour
{
    public TMP_InputField EmailInputField;
    public TMP_InputField PasswordInputField;
    public TMP_Text StatusText;

    [SerializeField] private PlayerProgressService _progressService;

    private const string API_KEY = "AIzaSyD3cn1bz2JJR-8V_z9fxeUkc44CV30wD_U";
    private const string DATABASE_URL = "https://telegram-mini-app-a32f1-default-rtdb.europe-west1.firebasedatabase.app/";

    void Start()
    {
        SetStatus("Ready");
    }

    public void SingInClick()
    {
        string email = EmailInputField.text;
        string password = PasswordInputField.text;

        if (!IsValidEmail(email))
        {
            SetStatus(email + " is not a valid email");
            return;
        }

        if (password.Length < 6 || password.Length > 64)
        {
            SetStatus("Password is too short or too long");
            return;
        }

        SetStatus("Logging in...");
        StartCoroutine(SignInRequest(email, password));
    }

    public void SingUpClick()
    {
        string email = EmailInputField.text;
        string password = PasswordInputField.text;

        if (!IsValidEmail(email))
        {
            SetStatus(email + " is not a valid email");
            return;
        }

        if (password.Length < 6 || password.Length > 64)
        {
            SetStatus("Password must be 6-64 characters");
            return;
        }

        SetStatus("Signing up...");
        StartCoroutine(SignUpRequest(email, password));
    }

    public void SignOutClick()
    {
        if (_progressService != null)
        {
            _progressService.ClearUser();
        }

        PlayerPrefs.DeleteKey("FirebaseToken");
        PlayerPrefs.DeleteKey("FirebaseUserId");
        SetStatus("User signed out");
    }

    private IEnumerator SignUpRequest(string email, string password)
    {
        string url = $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={API_KEY}";

        string jsonData = $"{{\"email\":\"{email}\",\"password\":\"{password}\",\"returnSecureToken\":true}}";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                string errorMessage = ParseErrorResponse(request.downloadHandler.text);
                SetStatus($"Sign up failed: {errorMessage}");
            }
            else
            {
                var response = JsonUtility.FromJson<SignUpResponse>(request.downloadHandler.text);

                if (_progressService != null)
                {
                    _progressService.SetUser(response.localId, response.idToken);

                    string playerName = email.Split('@')[0];
                    StartCoroutine(_progressService.UpdateLeaderboardName(playerName));
                }

                SaveUserData(response.localId, response.idToken);
                SetStatus($"Signed up successfully as {email}");
                StartCoroutine(WriteUserToDatabase(response.localId, email));
            }
        }
    }

    private IEnumerator SignInRequest(string email, string password)
    {
        string url = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={API_KEY}";

        string jsonData = $"{{\"email\":\"{email}\",\"password\":\"{password}\",\"returnSecureToken\":true}}";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                string errorMessage = ParseErrorResponse(request.downloadHandler.text);
                SetStatus($"Sign in failed: {errorMessage}");
            }
            else
            {
                var response = JsonUtility.FromJson<SignUpResponse>(request.downloadHandler.text);

                if (_progressService != null)
                {
                    _progressService.SetUser(response.localId, response.idToken);
                }

                SaveUserData(response.localId, response.idToken);
                SetStatus($"Signed in successfully as {email}");
                StartCoroutine(UpdateUserLastLogin(response.localId));
            }
        }
    }

    private IEnumerator WriteUserToDatabase(string userId, string email)
    {
        string url = $"{DATABASE_URL}users/{userId}.json";

        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string jsonData = $"{{\"email\":\"{email}\",\"createdAt\":{timestamp},\"lastLogin\":{timestamp}}}";

        using (UnityWebRequest request = new UnityWebRequest(url, "PUT"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("User data written to database");
            }
            else
            {
                Debug.LogError($"Failed to write to database: {request.error}");
            }
        }
    }

    private IEnumerator UpdateUserLastLogin(string userId)
    {
        string url = $"{DATABASE_URL}users/{userId}/lastLogin.json";
        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        using (UnityWebRequest request = new UnityWebRequest(url, "PUT"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(timestamp.ToString());
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();
        }
    }

    private void SaveUserData(string userId, string token)
    {
        PlayerPrefs.SetString("FirebaseUserId", userId);
        PlayerPrefs.SetString("FirebaseToken", token);
        PlayerPrefs.Save();
    }

    private string ParseErrorResponse(string responseText)
    {
        try
        {
            var errorResponse = JsonUtility.FromJson<ErrorResponse>(responseText);
            return errorResponse?.error?.message ?? "Unknown error";
        }
        catch
        {
            return "Unknown error";
        }
    }

    private void SetStatus(string msg)
    {
        Debug.Log(msg);
        if (StatusText != null)
        {
            StatusText.text = msg;
        }
    }

    private bool IsValidEmail(string email)
    {
        return !string.IsNullOrEmpty(email) && email.Contains("@") && email.Contains(".");
    }

    [Serializable]
    public class SignUpResponse
    {
        public string idToken;
        public string email;
        public string refreshToken;
        public string expiresIn;
        public string localId;
    }

    [Serializable]
    public class ErrorResponse
    {
        public ErrorDetail error;
    }

    [Serializable]
    public class ErrorDetail
    {
        public int code;
        public string message;
    }
}