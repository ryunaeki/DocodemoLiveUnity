using UnityEngine;
using System.Collections;
using WebSocketSharp;
using WebSocketSharp.Net;
using System;

[Serializable]
public class LiveMessage
{
    public string user;
    public string message;
}

public class LiveWebSocket : MonoBehaviour {

    WebSocket m_ws;
    UserObjectList m_uol;

    void Start()
    {
        m_uol = new UserObjectList();
        m_ws = new WebSocket("ws://docodemolive.mybluemix.net/ws/live");

        m_ws.OnOpen += (sender, e) =>
        {
            Debug.Log("WebSocket Open");
        };

        m_ws.OnMessage += (sender, e) =>
        {
            Debug.Log("WebSocket Message Type: " + e.Data);
        };

        m_ws.OnError += (sender, e) =>
        {
            Debug.Log("WebSocket Error Message: " + e.Message);
        };

        m_ws.OnClose += (sender, e) =>
        {
            Debug.Log("WebSocket Close");
        };

        m_ws.Connect();

        m_uol.StartDemo(5, 5);
    }

    void Update()
    {
        if (Input.GetKeyUp("s"))
        {
            LiveMessage lm = new LiveMessage()
            {
                user = "unity",
                message = "connect"
            };

            string json = JsonUtility.ToJson(lm);

            m_ws.Send(json);
        }

        m_uol.Update();
    }

    void OnDestroy()
    {
        m_ws.Close();
        m_ws = null;
    }
}
