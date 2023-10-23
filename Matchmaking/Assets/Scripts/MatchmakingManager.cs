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
    private Queue queue = new Queue();
    
    [SerializeField] private List<UIUserData> uIUsers = new List<UIUserData>();
    public Dictionary<string, string> usersInMatchmaking = new Dictionary<string, string>();
    public List<Dictionary<string, string>> roomsPlay = new List<Dictionary<string, string>>(); 

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        database = FirebaseDatabase.DefaultInstance.RootReference;

        FirebaseDatabase.DefaultInstance.GetReference("users/" + currentUser.userID + "/username").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.Log(task.Exception);
            }
            else if (task.IsCompleted)
            {
                currentUser.userUsername = (string)task.Result.Value;
                database.Child("matchmaking").Child(currentUser.userID).SetValueAsync(currentUser.userUsername);
                GetFriends();
            }
        });
        
    }

    private void Start()
    {
        foreach (var userUI in uIUsers)
            userUI.gameObject.SetActive(false);
    }

    private void Update()
    {
        switch (currentState)
        {
            case MatchmakingState.Init:
                currentState = queue.Peek().IsUnityNull() ? MatchmakingState.Init : MatchmakingState.Search;
                break;
            case MatchmakingState.Search:

                time += Time.deltaTime;
                counter = (int)time;
                tMPCounter.text = counter.ToString();
                if (time >= timeToCancelMatch)
                {
                    if (queue.Count > 1)
                    {
                        
                        //Crear sala si se acaba el tiempo de la sala
                    }
                    else
                    {
                        currentState = MatchmakingState.Expired;
                    }
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
                dataReference.ChildAdded += HandleChildRemoveMatchmaking;
            }
        });

    }

    private void HandleChildRemoveMatchmaking(object sender, ChildChangedEventArgs args)
    {
        if (args.Snapshot.Key != currentUser.userID)
        {
            
        }
    }

    private void HandleChildAddMatchmaking(object sender, ChildChangedEventArgs args)
    {
        if (args.Snapshot.Key != currentUser.userID)
        {
            usersInMatchmaking.Add(args.Snapshot.Key,(string)args.Snapshot.Value);
            
            
            //ya se puede crear la sala
        }
    }

    private void CancelMatchmaking()
    {
        database.Child("matchmaking").RemoveValueAsync();
        FirebaseManager.Instance.LoadScene(1);
    }



}
