using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using System;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;

public class FirebaseAuthUI : MonoBehaviour
{
    public TMP_InputField EmailInputField;
    public TMP_InputField PasswordInputField;

    public TMP_Text StatusText;

    private FirebaseAuth _auth;
    private DatabaseReference _dbRoot;

    private SynchronizationContext _unityContext;
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
    void Start()
    {
        SetStatus("Initializing Firebase");
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            var status = task.Result;
            if (status != DependencyStatus.Available)
            {
                RunOnUnityThread(() => { SetStatus(status.ToString()); });
            }
        });
        RunOnUnityThread(() =>
        {
            _auth = FirebaseAuth.DefaultInstance;
            _dbRoot = FirebaseDatabase.DefaultInstance.RootReference;
            SetStatus("Ready for user");
        });
    }
    public void SingInClick()
    {
        if (_auth == null)
        {
            SetStatus("Firebase not ready");
        }
        string email = EmailInputField.text;
        string password = PasswordInputField.text;
        if (!IsValidEmail(email))
        {
            SetStatus(email + "is not a valid email");
            return;
        }
        if (password.Length < 6 || password.Length > 64)
        {
            SetStatus("Password is too short or too long");
            return;
        }
        SetStatus("Loggin in");

        _auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWith(task =>
        {
            if (task.IsCanceled)
            {
                RunOnUnityThread(() => SetStatus("Sing in canselled"));
            }
            if (task.IsFaulted)
            {
                RunOnUnityThread(() => SetStatus($"Sing in failed: {FormatAuthError(task.Exception)}"));
            }
            FirebaseUser user = task.Result.User;
            RunOnUnityThread(() => SetStatus($"Singgen in succesfully as {user.Email}"));
            WriteUserRoDb(user);
        });
    }
    public void SingUpClick()
    {
        if (_auth == null)
        {
            SetStatus("Firebase not ready");
            return;
        }
        string email = EmailInputField.text;
        string password = PasswordInputField.text;
        if (!IsValidEmail(email))
        {
            SetStatus(email + "is not a valid email");
            return;
        }
        if (password.Length < 6 || password.Length > 64)
        {
            SetStatus("Password is too short or too long");
            return;
        }
        SetStatus("Loggin in");

        _auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWith(task =>
        {
            if (task.IsCanceled)
            {
                RunOnUnityThread(() => SetStatus("Sing up canselled"));
                return;
            }
            if (task.IsFaulted)
            {
                RunOnUnityThread(() => SetStatus($"Sing up failed: {FormatAuthError(task.Exception)}"));
                return;
            }
            FirebaseUser user = task.Result.User;
            RunOnUnityThread(() => SetStatus($"Singgen up succesfully as {user.Email}"));
            WriteUserRoDb(user);
        });
    }
    public void SignOutClick()
    {
        if (_auth != null)
        {
            _auth.SignOut();
            SetStatus("User signed out");
        }
        else
        {
            SetStatus("Firebase not initialized");
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
    private string FormatAuthError(AggregateException ex)
    {
        if (ex == null)
        {
            return "Uknown error";
        }
        return ex.GetBaseException().Message;
    }

    private void WriteUserRoDb(FirebaseUser user)
    {
        var userData = new Dictionary<string, object>
        {
            {"email" , user.Email ?? "" },
            { "createdAt" , DateTimeOffset.UtcNow.ToUnixTimeSeconds() }
        };

        _dbRoot.Child("users").Child(user.UserId).UpdateChildrenAsync(userData).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.Log("DbWrite failed");
                return;
            }
            Debug.Log("Write Ok");
        });
    }
}
