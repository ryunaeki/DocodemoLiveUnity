using UnityEngine;
using System.Collections;
using WebSocketSharp;
using WebSocketSharp.Net;
using System;

[Serializable]
public class LiveMessage
{
    public string userid;
    public string username;
    public string action;
    public string message;
}

public class LiveWebSocket : MonoBehaviour {

    WebSocket m_ws;
    UserObjectList m_uol;

    delegate void ConnectWebSocketDelegate();

    void Start()
    {
        m_uol = new UserObjectList(5);

        ConnectWebSocket();

        // デモ用
        // m_uol.StartDemo(25);
    }

    void Update()
    {
        if (Input.GetKeyUp("s"))
        {
            LiveMessage lm = new LiveMessage()
            {
                userid = "unity",
                username = "unity",
                action = "keypress",
                message = "s"
            };

            string json = JsonUtility.ToJson(lm);

            m_ws.Send(json);
        }

        if (m_uol != null)
        {
            m_uol.Update();
        }
    }
    
    void OnDestroy()
    {
        m_ws.Close();
        m_ws = null;
    }

    void ConnectWebSocket()
    {
        m_ws = new WebSocket("ws://docodemolive.mybluemix.net/ws/live");

        m_ws.OnOpen += (sender, e) =>
        {
            Debug.Log("WebSocket Open");
        };

        m_ws.OnMessage += (sender, e) =>
        {
            Debug.Log("WebSocket Message Type: " + e.Data);

            string strJson = e.Data;
            m_uol.AddJsonQueue(strJson);
        };

        m_ws.OnError += (sender, e) =>
        {
            Debug.Log("WebSocket Error Message: " + e.Message);
        };

        m_ws.OnClose += (sender, e) =>
        {
            Debug.Log("WebSocket Close");

            ConnectWebSocketDelegate con = new ConnectWebSocketDelegate(ConnectWebSocket);
            con.Invoke();
        };

        m_ws.Connect();
    }
}
