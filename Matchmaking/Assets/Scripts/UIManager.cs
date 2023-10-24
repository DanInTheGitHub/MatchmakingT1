using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; } = null;

    public GameObject friendsPanel, notificationSend, notificationRecived;
    public TextMeshProUGUI tMPNotification, tMPUserData;
    public UIUserData[] usersList;
    List<Vector3> positions = new List<Vector3>();

    int lastIndex;
    string username, id;

    void Awake()
    {
        if(Instance != null)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        SetPos();
    }

    void Update()
    {
        for(int i = 0; i<usersList.Length; i++)
        {
            if(!usersList[i].inUse)
            {
                usersList[i].gameObject.SetActive(false);
                
                if(i+1 < usersList.Length)
                {
                    if(usersList[i+1].inUse)
                        (usersList[i+1], usersList[i]) = (usersList[i], usersList[i+1]);
                }

                continue;
            }
            else
            {
                usersList[i].gameObject.SetActive(true);
                
                if(i+1 < usersList.Length)
                {
                    if(usersList[i+1].friend && usersList[i+1].inUse && !usersList[i].friend)
                        (usersList[i+1], usersList[i]) = (usersList[i], usersList[i+1]);
                }
            }
            usersList[i].GetComponent<RectTransform>().localPosition = positions[i];
            //last_index = users[i].index;
        }
    }

    public void Friend_Panel_Activation()
    {
        if(friendsPanel.activeInHierarchy)
        {
            friendsPanel.SetActive(false);
        }
        else
        {
            friendsPanel.SetActive(true);
        }
    }

    public void Update_User_Info(string username)
    {
        tMPUserData.text = username;
    }

    public void Update_Users(string ID, string username, bool friend)
    {
        if(ID == FirebaseManager.Instance.currentUser.userID) return;

        foreach(UIUserData user in usersList)
        {
            if(user.id == ID)
            {
                user.friend = friend;
                if(friend)user.image.color = Color.green;
                break;
            }

            if(!user.inUse)
            {
                user.txt.text = username;
                user.id = ID;
                user.inUse = true;
                user.friend = friend;
                user.image.color = friend ? Color.green : Color.white;
                break;
            }
        }
    }

    public void Remove_User(string ID)
    {
        foreach(UIUserData user in usersList)
        {
            if(user.id == ID)
            {
                user.inUse = false;
                break;
            }
        }
    }

    public void Notification_Send_Activation()
    {
        if(notificationSend.activeInHierarchy)
        {
            notificationSend.SetActive(false);
        }
        else
        {
            notificationSend.SetActive(true);
            Invoke(nameof(Notification_Send_Activation),3);
        }
    }

    public void Notification_Receved(string id, string username)
    {
        notificationRecived.SetActive(true);
        tMPNotification.text = "Solicitud de amistad de: " + username;
        this.username = username;
        this.id = id;
    }

    public void Confirmation(bool confirm)
    {
        if(confirm)
            FirebaseManager.Instance.Acept_Request(id,username);
        else
            FirebaseManager.Instance.Decline_Request();

        notificationRecived.SetActive(false);
        username = string.Empty;
        id = string.Empty;
    }

    public void SetPos()
    {
        float scale = 0;
        for( int i = 0; i< usersList.Length; i++)
        {
            usersList[i].index=i;
            positions.Add(new Vector3(6,usersList[i].GetComponent<RectTransform>().localPosition.y - scale,0));
        }
    }
}
