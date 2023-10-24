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

public class MatchmakingManager : MonoBehaviour
{
    public enum MatchmakingState { Init, Search, Expired }
    [HideInInspector] public MatchmakingState currentState = MatchmakingState.Init;
    public static MatchmakingManager Instance;

    private DatabaseReference database;
    [SerializeField] TMP_Text tMPCounter;
    [HideInInspector] public User currentUser;
    private float time = 0, timeToCancelMatch = 120f;
    private int counter;
    [SerializeField] private Button startRoom, cancelMatch;
    
    [SerializeField] private List<UIUserData> uIUsers = new List<UIUserData>();
    public Dictionary<string, string> usersInMatchmaking = new Dictionary<string, string>();
    public List<Dictionary<string, string>> roomsPlay = new List<Dictionary<string, string>>(); 

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        foreach (var userUI in uIUsers)
            userUI.gameObject.SetActive(false);
        
        startRoom.gameObject.SetActive(false);
        startRoom.onClick.AddListener(CreateRoom);
        cancelMatch.onClick.AddListener(CancelMatchmaking);
        database = FirebaseDatabase.DefaultInstance.RootReference;
        FirebaseAuth.DefaultInstance.StateChanged += Check_Login;
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
            .GetReference("users/" + currentUser.userID + "/username")
            .GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.Log(task.Exception);
                }
                else if (task.IsCompleted)
                {
                    currentUser.userUsername = (string)task.Result.Value;
                    FirebaseDatabase.DefaultInstance.RootReference.Child("matchmaking").Child(currentUser.userID).SetValueAsync(currentUser.userUsername);
                    GetFriends();
                }
            });
    }
    

    private void Update()
    {
        switch (currentState)
        {
            case MatchmakingState.Init:
                currentState = usersInMatchmaking.Count > 0 ? MatchmakingState.Init : MatchmakingState.Search;
                break;
            case MatchmakingState.Search:

                time += Time.deltaTime;
                counter = (int)time;
                tMPCounter.text = counter.ToString();
                if (time >= timeToCancelMatch)
                {
                    if (usersInMatchmaking.Count > 1)
                        CreateRoom();
                    else
                        currentState = MatchmakingState.Expired;
                }
                else
                {
                    if (usersInMatchmaking.Count > 1)
                        startRoom.gameObject.SetActive(true);
                    else
                        startRoom.gameObject.SetActive(false);
                }

                break;
            case MatchmakingState.Expired:

                CancelMatchmaking();

                break;
        }
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
                usersInMatchmaking.Add(currentUser.userID, currentUser.userUsername);
                
                var dataReference = FirebaseDatabase.DefaultInstance.GetReference("matchmaking");
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
                usersInMatchmaking.Remove(args.Snapshot.Key);
            }
        }
        
    }

    private void HandleChildAddMatchmaking(object sender, ChildChangedEventArgs args)
    {
        if (args.Snapshot.Key != currentUser.userID)
        {
            usersInMatchmaking.Add(args.Snapshot.Key,(string)args.Snapshot.Value);
            uIUsers[usersInMatchmaking.Count-1].txt.text = (string)args.Snapshot.Value;
            uIUsers[usersInMatchmaking.Count-1].button.gameObject.SetActive(true);
            uIUsers[usersInMatchmaking.Count-1].gameObject.SetActive(true);
        }
    }

    public void CreateRoom()
    {
        foreach (var key in usersInMatchmaking)
            database.Child("room01").Child(key.Key).SetValueAsync(key.Value);
        database.Child("matchmaking").RemoveValueAsync();
        usersInMatchmaking.Clear();
        SceneManager.LoadScene(3);
    }
    
    private void CancelMatchmaking()
    {
        database.Child("matchmaking").Child(currentUser.userID).RemoveValueAsync();
        usersInMatchmaking.Remove(currentUser.userID);
        SceneManager.LoadScene(1);
    }
}
