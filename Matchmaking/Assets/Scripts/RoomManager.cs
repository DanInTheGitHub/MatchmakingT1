using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;
public class RoomManager : MonoBehaviour
{
    [SerializeField] private Button exitRoom;
    [HideInInspector] public User currentUser;
    [SerializeField] private List<UIUserData> uIUsers = new List<UIUserData>();
    public Dictionary<string, string> usersInRoom = new Dictionary<string, string>();
    private DatabaseReference database;
    
    void Awake()
    { 
        foreach (var userUI in uIUsers)
            userUI.gameObject.SetActive(false);
        database = FirebaseDatabase.DefaultInstance.RootReference;
        FirebaseAuth.DefaultInstance.StateChanged += Check_Login;
        exitRoom.onClick.AddListener(ExitRoom);
    }
    
    private void Check_Login(object sender, EventArgs e)
    {
        if (FirebaseAuth.DefaultInstance.CurrentUser != null)
        {
            currentUser = new User
            {
                userID = FirebaseAuth.DefaultInstance.CurrentUser.UserId
            };
            GetUser_Username();
        }
    }
    
    public void GetUser_Username()
    {
        FirebaseDatabase.DefaultInstance
            .GetReference("users/" + currentUser.userID + "/username").GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.Log(task.Exception);
                }
                else if (task.IsCompleted)
                {
                    currentUser.userUsername = (string)task.Result.Value;
                    //FirebaseDatabase.DefaultInstance.RootReference.Child("room01").Child(currentUser.userID).SetValueAsync(currentUser.userUsername);
                    GetFriends();
                }
            });
    }
    
    private void GetFriends()
    {
        FirebaseDatabase.DefaultInstance.GetReference("users/" + currentUser.userID + "/friends").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.Log(task.Exception);
            }
            else if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;

                foreach (var userFriend in (Dictionary<string, object>)snapshot.Value)
                {
                    currentUser.userFriends.Add(userFriend.Key, (string)userFriend.Value);
                }
                
                uIUsers[0].txt.text = currentUser.userUsername;
                uIUsers[0].button.gameObject.SetActive(false);
                uIUsers[0].gameObject.SetActive(true);
                usersInRoom.Add(currentUser.userID, currentUser.userUsername);
                
                var dataReference = FirebaseDatabase.DefaultInstance.GetReference("room01");
                dataReference.ChildAdded += HandleChildAddMatchmaking;
                dataReference.ChildRemoved += HandleChildRemoveMatchmaking;
            }
        });

    }

    private void HandleChildRemoveMatchmaking(object sender, ChildChangedEventArgs args)
    {
        foreach (var uIUser in uIUsers)
        {
            if (uIUser.txt.text == (string)args.Snapshot.Value)
            {
                uIUser.txt.text = "";
                uIUser.gameObject.SetActive(false);
            }
        }
        usersInRoom.Remove(args.Snapshot.Key);
    }

    private void HandleChildAddMatchmaking(object sender, ChildChangedEventArgs args)
    {
        if (args.Snapshot.Key != currentUser.userID)
        {
            usersInRoom.Add(args.Snapshot.Key,(string)args.Snapshot.Value);
            uIUsers[usersInRoom.Count-1].txt.text = (string)args.Snapshot.Value;
            uIUsers[usersInRoom.Count-1].button.gameObject.SetActive(true);
            uIUsers[usersInRoom.Count-1].gameObject.SetActive(true);
        }
    }
    
    private void ExitRoom()
    {
        database.Child("room01").Child(currentUser.userID).RemoveValueAsync();
        usersInRoom.Remove(currentUser.userID);
        SceneManager.LoadScene(1);
    }
}
