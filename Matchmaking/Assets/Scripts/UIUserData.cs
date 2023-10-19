using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIUserData : MonoBehaviour
{
    public Image image;
    public TextMeshProUGUI txt;
    public Button button;
    public int index;
    public bool inUse;
    public bool friend;
    public string id;

    void Start()
    {
        button.onClick.AddListener(SendRequest);
    }

    public void SendRequest()
    {
        FirebaseManager.Instance.SendFriendRequest(id);
    }
}
