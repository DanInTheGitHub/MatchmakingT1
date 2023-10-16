using System.Collections;
using UnityEngine;
using Firebase.Auth;
using Firebase.Database;
using TMPro;
using System;
using UnityEngine.SceneManagement;
using Firebase.Extensions;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;

public class FireBase_Interactions : MonoBehaviour
{
    [SerializeField] private TMP_InputField Email_TXT,Username_TXT,Password_TXT;
    private DatabaseReference database;
    public User currentUser;
    public static FireBase_Interactions Instance {get;private set;} = null;
    public Dictionary<string,string> Users_Online = new Dictionary<string, string>();

    void Awake()
    {
        if(Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        FirebaseAuth.DefaultInstance.StateChanged += Check_Login;
        database = FirebaseDatabase.DefaultInstance.RootReference;
    }

    private void Check_Login(object sender, EventArgs e)
    {
        if (FirebaseAuth.DefaultInstance.CurrentUser != null)
        {
            int scene = SceneManager.GetActiveScene().buildIndex;
            if(scene != 1)
            {
                Load_Sceen(1);
            }
            else
            {
                currentUser = new User
                {
                    userID = FirebaseAuth.DefaultInstance.CurrentUser.UserId
                };
                GetUser_Username();
                
            }
            
        }
    }
    public void Sing_Up()
    {
        string user_Email = Email_TXT.text;
        string user_Password = Password_TXT.text;
        StartCoroutine(RegisterUser(user_Email,user_Password));
    } 
    public void Log_In()
    {
        var auth = FirebaseAuth.DefaultInstance;
        auth.SignInWithEmailAndPasswordAsync(Email_TXT.text, Password_TXT.text).ContinueWith(task => {
            if (task.IsCanceled)
            {
                Debug.LogError("Sign In With Email And Password Async was canceled.");
                return;
            }
            if (task.IsFaulted)
            {
                Debug.LogError("Sign In With Email And Password Async encountered an error: " + task.Exception);
                return;
            }
            AuthResult result = task.Result;
        });
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
                    currentUser.user_Username = (string) task.Result.Value;
                    FirebaseDatabase.DefaultInstance.RootReference.Child("users-online").Child(currentUser.userID).SetValueAsync(currentUser.user_Username);
                    UI_Manager.Instance.Update_User_Info(currentUser.user_Username);
                    Get_Friends();
                }
            });
    }
    public void Log_Out()
    {
        FirebaseDatabase.DefaultInstance.RootReference.Child("users-online").Child(currentUser.userID).RemoveValueAsync();
        currentUser.userID = null;
        PlayerPrefs.SetString(nameof(currentUser.user_Username),string.Empty);
        PlayerPrefs.SetString(nameof(currentUser.userID),string.Empty);
        FirebaseAuth.DefaultInstance.SignOut();
        Load_Sceen(0);
    }
    public void Load_Sceen(int i)
    {
        SceneManager.LoadScene(i);
    }
    public void Reset_Password()
    {
        string email = Email_TXT.text;
        StartCoroutine(Restor_Password(email));
    }
    private void Get_Friends()
    {
        FirebaseDatabase.DefaultInstance
            .GetReference("users/" + currentUser.userID + "/friends")
            .GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.Log(task.Exception);
                }
                else if (task.IsCompleted)
                {
                    DataSnapshot snapshot = task.Result;

                    foreach(var userFriend in (Dictionary<string,object>) snapshot.Value)
                    {
                        currentUser.user_Firends.Add(userFriend.Key,(string)userFriend.Value);
                    }

                    var databaseRef = FirebaseDatabase.DefaultInstance.GetReference("users-online");
                    databaseRef.ChildAdded += HandleChildAdded;
                    databaseRef.ChildRemoved += HandleChildRemoved;

                    var databaseRef2 = FirebaseDatabase.DefaultInstance.GetReference("request");
                    databaseRef2.ChildAdded += HandleChildAdded_Friend;

                    var databaseRef3 = FirebaseDatabase.DefaultInstance.GetReference("accepted");
                    databaseRef3.ChildAdded += HandleChildAdded_Requested_Friend;
                }
            });
    }
    private IEnumerator Restor_Password(string email)
    {
        var auth = FirebaseAuth.DefaultInstance;
        var resetTask = auth.SendPasswordResetEmailAsync(email);

        yield return new WaitUntil(() => resetTask.IsCompleted);

        if (resetTask.IsCanceled)
        {
            Debug.LogError($"SendPasswordResetEmailAsync is canceled");
        }
        else if (resetTask.IsFaulted)
        {
            Debug.LogError($"SendPasswordResetEmailAsync encountered error" + resetTask.Exception);
        }
        else
        {
            Debug.Log("Password reset email sent successfully to: " + email);
        }
    }
    private IEnumerator RegisterUser(string email, string password)
    {
        var auth = FirebaseAuth.DefaultInstance;
        var registerTask = auth.CreateUserWithEmailAndPasswordAsync(email, password);

        yield return new WaitUntil(() => registerTask.IsCompleted);

        if (registerTask.IsCanceled)
        {
            Debug.LogError($"Create User With Email And Passwor dAsync is canceled");
        }
        else if (registerTask.IsFaulted)
        {
            Debug.LogError($"Create User With Email And Password Async encountered error" + registerTask.Exception);
        }
        else
        {
            AuthResult result = registerTask.Result;
            string name = Username_TXT.text;
            User user = new User();
            user.user_Firends.Add(result.User.UserId,name);
            database.Child("users").Child(result.User.UserId).Child("username").SetValueAsync(name);
            database.Child("users").Child(result.User.UserId).Child("friends").SetValueAsync(user.user_Firends);
        }
    }
    private void OnDestroy()
    {
        FirebaseAuth.DefaultInstance.StateChanged -= Check_Login;
    }
    private void OnApplicationQuit()
    { 
        if(currentUser.userID != null)
        {
            FirebaseDatabase.DefaultInstance.RootReference.Child("users-online").Child(currentUser.userID).RemoveValueAsync();
            currentUser.userID = null;
        }
        
    }
    void HandleChildAdded(object sender, ChildChangedEventArgs args)
    {   
        if(args.Snapshot.Key != currentUser.userID)
        {
            Users_Online.Add(args.Snapshot.Key,(string)args.Snapshot.Value);
            UI_Manager.Instance.Update_Users(args.Snapshot.Key,(string)args.Snapshot.Value, currentUser.user_Firends.ContainsKey(args.Snapshot.Key));  
        }
    }
    void HandleChildRemoved(object sender, ChildChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }
        Users_Online.Remove(args.Snapshot.Key);
        UI_Manager.Instance.Remove_User(args.Snapshot.Key,(string)args.Snapshot.Value); 
    }
    void HandleChildAdded_Friend(object sender, ChildChangedEventArgs args)
    {
        if(args.Snapshot.Key == currentUser.userID)
        {
            var user = (Dictionary<string,object>) args.Snapshot.Value;
            var user_ID = user.Keys.ToArray();
            var user_Username = user.Values.ToArray();
            UI_Manager.Instance.Notification_Receved(user_ID[0],(string)user_Username[0]);
        }        
    }

    void HandleChildAdded_Requested_Friend(object sender, ChildChangedEventArgs args)
    {
        Debug.Log(args.Snapshot.Key);
        Debug.Log(currentUser.userID);
        if(args.Snapshot.Key == currentUser.userID)
        {
            var user = (Dictionary<string,object>) args.Snapshot.Value;
            var user_ID = user.Keys.ToArray();
            var user_Username = user.Values.ToArray();
            Request_Acepted(user_ID[0],(string)user_Username[0]);
        }        
    }

    public void Acept_Request(string ID, string username)
    {
        currentUser.user_Firends.Add(ID,username);
        if(Users_Online.ContainsKey(ID)) UI_Manager.Instance.Update_Users(ID,username,true);
        database.Child("users").Child(currentUser.userID).Child("friends").SetValueAsync(currentUser.user_Firends);
        database.Child("request").Child(currentUser.userID).RemoveValueAsync();
        database.Child("accepted").Child(ID).Child(currentUser.userID).SetValueAsync(currentUser.user_Username);
    }

    public void Request_Acepted(string ID, string username)
    {
        currentUser.user_Firends.Add(ID,username);
        if(Users_Online.ContainsKey(ID)) UI_Manager.Instance.Update_Users(ID,username,true);
        database.Child("users").Child(currentUser.userID).Child("friends").SetValueAsync(currentUser.user_Firends);
        database.Child("accepted").Child(currentUser.userID).RemoveValueAsync();
    }

    public void Decline_Request()
    {
        database.Child("request").Child(currentUser.userID).RemoveValueAsync();
    }

    public void Send_Frend_Request(string user_ID)
    {
        database.Child("request").Child(user_ID).Child(currentUser.userID).SetValueAsync(currentUser.user_Username);
        UI_Manager.Instance.Notification_Send_Activation();
    }
}

[System.Serializable]
public class User
{
    public string userID;
    public string user_Username;
    public Dictionary<string, string> user_Firends;

    public User()
    {
        user_Firends = new Dictionary<string, string>();
    }
}
