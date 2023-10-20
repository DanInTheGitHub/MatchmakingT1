using Firebase.Database;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SocialPlatforms;

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
    [SerializeField] private List<UIUserData> players = new List<UIUserData>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
            Destroy(gameObject);
    }
    private void Start()
    {
        foreach (var userUI in players)
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

                    }
                    else
                    {
                        currentState = MatchmakingState.Expired;
                    }
                }

                break;
            case MatchmakingState.Expired:

                time = 0;

                break;
        }
    }

    public void CancelMatchmaking()
    {
        database.Child("matchmaking").Child(currentUser.userID).RemoveValueAsync();
        FirebaseManager.Instance.LoadScene(1);
        SearchingMatch();
    }

    public void SearchingMatch()
    {
        var dataReference = FirebaseDatabase.DefaultInstance.GetReference("matchmaking");
        database.ChildAdded += HandleChildAddMatchmaking;
    }

    private void HandleChildAddMatchmaking(object sender, ChildChangedEventArgs args)
    {
        if (args.Snapshot.Key != currentUser.userID)
        {
            
        }
    }


}
