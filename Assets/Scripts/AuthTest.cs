//using Firebase;
//using Firebase.Auth;
//using Firebase.Database;
//using Firebase.Extensions;
using System;
using System.Collections.Generic;
using UnityEngine;


public class AuthTest : MonoBehaviour
{
    //public string email = "Test@gmail.com";
    //public string password = "password123";

    //private FirebaseAuth _auth;
    //private DatabaseReference _dbRoot;

    //private void Start()
    //{
    //    FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
    //    {
    //        var status = task.Result;
    //        if (status != DependencyStatus.Available)
    //        {
    //            Debug.LogWarning("FB not avaliable");
    //        }
    //        _auth = FirebaseAuth.DefaultInstance;
    //        _dbRoot = FirebaseDatabase.DefaultInstance.RootReference;
    //        CreateOrSingInAndWriteUser(email, password);
    //    });
    //}

    //public void CreateOrSingInAndWriteUser(string userName, string userPassword)
    //{
    //    _auth.CreateUserWithEmailAndPasswordAsync(userName, userPassword).ContinueWithOnMainThread(task =>
    //    {
    //        if (task.IsCanceled || task.IsFaulted)
    //        {
    //            Debug.Log("user maybe exists");
    //            SingInAndWriteUser(userName, userPassword);
    //            return;
    //        }
    //        var user = task.Result.User;

    //        Debug.Log("User Created");
    //        WriteUserRoDb(user);
    //    });
    //}
    //public void SingInAndWriteUser(string userName, string userPassword)
    //{
    //    _auth.SignInWithEmailAndPasswordAsync(userName, userPassword).ContinueWithOnMainThread(task =>
    //    {
    //        if (task.IsCanceled || task.IsFaulted)
    //        {
    //            Debug.Log("Sing in failed");
    //            return;
    //        }
    //        var user = task.Result.User;
    //        Debug.Log("Singned in");
    //        WriteUserRoDb(user);
    //    });
    //}
    //private void WriteUserRoDb(FirebaseUser user)
    //{
    //    var userData = new Dictionary<string, object>
    //    {
    //        {"email" , user.Email ?? "" },
    //        { "createdAt" , DateTimeOffset.UtcNow.ToUnixTimeSeconds() }
    //    };

    //    _dbRoot.Child("users").Child(user.UserId).UpdateChildrenAsync(userData).ContinueWithOnMainThread(task =>
    //    {
    //        if (task.IsCanceled || task.IsFaulted)
    //        {
    //            Debug.Log("DbWrite failed");
    //            return;
    //        }
    //        Debug.Log("Write Ok");
    //    });
    //}
}



