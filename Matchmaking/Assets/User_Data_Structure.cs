using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class User_Data_Structure : MonoBehaviour
{
    public Image image;
    public TextMeshProUGUI txt;
    public Button button;
    public int index;
    public bool In_Use;
    public bool friend;
    public string id;

    void Start()
    {
        button.onClick.AddListener(sendRequest);
    }

    public void sendRequest()
    {
        FireBase_Interactions.Instance.Send_Frend_Request(id);
    }
}
