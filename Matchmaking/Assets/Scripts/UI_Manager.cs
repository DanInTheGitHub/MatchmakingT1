using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public class UI_Manager : MonoBehaviour
{
    public GameObject Friend_Panel, Notification_Send, Notification_recived;
    public TextMeshProUGUI Notification_recived_TXT, user_Info_TXT;
    public User_Data_Structure[] users;
    List<Vector3> positions = new List<Vector3>();
    public static UI_Manager Instance {get;private set;} = null;
    int last_index;
    string username,id;
    void Awake()
    {
        if(Instance != null)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        Set_pos();
    }

    void Update()
    {
        for(int i = 0; i<users.Length; i++)
        {
            if(!users[i].In_Use)
            {
                users[i].gameObject.SetActive(false);
                
                if(i+1 < users.Length)
                {
                    if(users[i+1].In_Use)
                    {
                        var j = users[i+1];
                        users[i+1] = users[i];
                        users[i] = j;
                    }
                }

                continue;
            }
            else
            {
                users[i].gameObject.SetActive(true);
                
                if(i+1 < users.Length)
                {
                    if(users[i+1].friend && users[i+1].In_Use && !users[i].friend)
                    {
                        var j = users[i+1];
                        users[i+1] = users[i];
                        users[i] = j;
                    }
                }
            }

            

            users[i].GetComponent<RectTransform>().localPosition = positions[i];
            //last_index = users[i].index;
        }
    }

    public void Friend_Panel_Activation()
    {
        if(Friend_Panel.activeInHierarchy)
        {
            Friend_Panel.SetActive(false);
        }
        else
        {
            Friend_Panel.SetActive(true);
        }
    }

    public void Update_User_Info(string username)
    {
        user_Info_TXT.text = username;
    }

    public void Update_Users(string ID, string username, bool friend)
    {
        if(ID == FireBase_Interactions.Instance.currentUser.userID) return;

        foreach(User_Data_Structure user in users)
        {
            if(user.id == ID)
            {
                user.friend = friend;
                if(friend)user.image.color = Color.green;
                break;
            }

            if(!user.In_Use)
            {
                user.txt.text = username;
                user.id = ID;
                user.In_Use = true;
                user.friend = friend;
                if(friend)user.image.color = Color.green;
                break;
            }
        }
    }

    public void Remove_User(string ID, string username)
    {
        foreach(User_Data_Structure user in users)
        {
            if(user.id == ID)
            {
                user.In_Use = false;
                break;
            }
        }
    }

    public void Notification_Send_Activation()
    {
        if(Notification_Send.activeInHierarchy)
        {
            Notification_Send.SetActive(false);
        }
        else
        {
            Notification_Send.SetActive(true);
            Invoke(nameof(Notification_Send_Activation),3);
        }
    }

    public void Notification_Receved(string id, string username)
    {
        Notification_recived.SetActive(true);
        Notification_recived_TXT.text = "Solicitud de amistad de: " + username;
        this.username = username;
        this.id = id;
    }

    public void Confirmation(bool b)
    {
        if(b){FireBase_Interactions.Instance.Acept_Request(id,username);}else{FireBase_Interactions.Instance.Decline_Request();}
        Notification_recived.SetActive(false);
        username = string.Empty;
        id = string.Empty;
    }

    public void Set_pos()
    {
        float scale = 0;
        for( int i = 0; i< users.Length; i++)
        {
            users[i].index=i;
            positions.Add(new Vector3(6,users[i].GetComponent<RectTransform>().localPosition.y - scale,0));
        }
    }
}
