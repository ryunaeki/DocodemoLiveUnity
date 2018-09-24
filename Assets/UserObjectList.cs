using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using VolumetricLines;

public class UserObjectList  {
    public const float NM_POPUP_TIMEOUT = 5.0f;
    public const float NM_POPUP_POS_Y = 70.0f;
    public const float NM_LINE_SPEED = 10.0f;

    public const float NM_EVENT_TIMEOUT = 0.3f;

    int m_nMaxX;
    OrderedDictionary m_dicPenLight;
    List<string> m_listJsonQueue;
    private float m_fTimeElapsed;
    public bool m_bDemo;

    public class UserObject
    {
        public enum VolumetricLineType
        {
            Stop,
            Back,
            Front1,
            Front2,
            Return
        };

        public int m_nIndex;
        public VolumetricLineType m_vlType;
        public float m_fLastAngle;
        public GameObject m_goVolumetricLine;
        public GameObject m_goName;
        public GameObject m_goPopup;

        public UserObject(int nIndex)
        {
            m_nIndex = nIndex;
            m_vlType = VolumetricLineType.Stop;
            m_fLastAngle = 90;
        }
    }

    public void AddJsonQueue(string strJson)
    {
        m_listJsonQueue.Add(strJson);
    }

    public UserObjectList(int nMaxX)
    {
        m_nMaxX = nMaxX;

        m_dicPenLight = new OrderedDictionary();
        m_listJsonQueue = new List<string>();
        m_fTimeElapsed = 0;
        m_bDemo = false;
    }

    public void Update()
    {
        if (m_bDemo)
        {
            m_fTimeElapsed += Time.deltaTime;
            if (m_fTimeElapsed >= NM_EVENT_TIMEOUT)
            {
                System.Random ran = new System.Random();
                int nPos = ran.Next(m_dicPenLight.Keys.Count - 1);
                string strID = (string)m_dicPenLight.Cast<DictionaryEntry>().ElementAt(nPos).Key;
                UserObject uo = GetUserObject(strID);

                StartVolumetricLine(uo);

                m_fTimeElapsed = 0.0f;
            }
        }

        foreach (string strJson in m_listJsonQueue)
        {
            MessageEvent(strJson);
        }
        m_listJsonQueue.Clear();

        foreach (UserObject uo in m_dicPenLight.Values)
        {
            UpdateVolumetricLine(uo);
            UpdatePopup(uo);
        }
    }

    private void MessageEvent(string strJson)
    {
        LiveMessage lm = JsonUtility.FromJson<LiveMessage>(strJson);
        UserObjectList.UserObject uo = GetUserObject(lm.userid);
        if (uo == null)
        {
            uo = Add(lm.userid);
        }

        SetUserName(uo, lm.username);

        switch (lm.action)
        {
            case "penlight":
                {
                    Color col = Color.white;
                    switch (lm.message)
                    {
                        case "red":
                            col = Color.red;
                            break;
                        case "magenta":
                            col = Color.magenta;
                            break;
                        case "yellow":
                            col = Color.yellow;
                            break;
                        case "blue":
                            col = Color.blue;
                            break;
                        case "cyan":
                            col = Color.cyan;
                            break;
                        case "green":
                            col = Color.green;
                            break;
                    }
                    SetColor(uo, col);
                }
                StartVolumetricLine(uo);
                break;
            case "call":
                CreatePopup(uo, lm.message);
                break;
        }
    }

    private Vector3 GetVolumetricLinePosition(int nIndex)
    {
        int i = nIndex / m_nMaxX;
        int j = nIndex % m_nMaxX;

        float x = (((float)j - ((float)m_nMaxX / 2.0f)) * 150.0f);
        float y = 0.0f;
        float z = 150.0f + ((float)i * 130.0f);

        return new Vector3(x, y, z);
    }

    public void StartDemo(int nCount)
    {
        m_bDemo = true;

        System.Random ran = new System.Random();

        for (int u = 0; u < nCount; u++)
        {
            Guid guidValue = Guid.NewGuid();
            string strUserName = string.Format("ファン{0}", u+1);

            Vector3 pos = GetVolumetricLinePosition(u);
            UserObject uo = CreateVolumetricLine(guidValue.ToString(), u, pos);
            SetUserName(uo, strUserName);

            Color col = Color.cyan;
            switch (ran.Next(6))
            {
                case 1:
                    col = Color.blue;
                    break;
                case 2:
                    col = Color.green;
                    break;
                case 3:
                    col = Color.magenta;
                    break;
                case 4:
                    col = Color.red;
                    break;
                case 5:
                    col = Color.white;
                    break;
                case 6:
                    col = Color.yellow;
                    break;
                default:
                    col = Color.cyan;
                    break;
            }
            SetColor(uo, col);
        }
    }

    private void Delete(string strTargetID)
    {
        UserObject uo = GetUserObject(strTargetID);

        try
        {
            UnityEngine.Object.Destroy(uo.m_goPopup, 0);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine(ex.Message);
            System.Diagnostics.Trace.WriteLine(ex.StackTrace);
        }

        try
        {
            UnityEngine.Object.Destroy(uo.m_goName, 0);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine(ex.Message);
            System.Diagnostics.Trace.WriteLine(ex.StackTrace);
        }

        try
        {
            UnityEngine.Object.Destroy(uo.m_goVolumetricLine, 0);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine(ex.Message);
            System.Diagnostics.Trace.WriteLine(ex.StackTrace);
        }

        m_dicPenLight.Remove(strTargetID);
    }

    private void StopDemo()
    {
        m_bDemo = false;

        foreach (string strID in m_dicPenLight.Keys)
        {
            Delete(strID);
        }
    }

    private UserObject GetUserObject(string strID)
    {
        if (!m_dicPenLight.Contains(strID))
            return null;

        return (UserObject)m_dicPenLight[strID];
    }

    private void StartVolumetricLine(UserObject uo)
    {
        if (uo.m_vlType != UserObject.VolumetricLineType.Stop)
            return;

        uo.m_vlType = UserObject.VolumetricLineType.Back;
    }

    private int GetMinIndex()
    {
        const int MAX_INDEX = 9999;
        int nMinIndex = MAX_INDEX;

        foreach (UserObject uo in m_dicPenLight.Values)
        {
            if (uo.m_nIndex < MAX_INDEX)
                nMinIndex = uo.m_nIndex;
        }

        if (nMinIndex == MAX_INDEX)
        {
            nMinIndex = 0;
        }else
        {
            nMinIndex = nMinIndex + 1;
        }

        return nMinIndex;
    }

    private UserObject Add(string strID)
    {
        int nIndex = GetMinIndex();
        Vector3 pos = GetVolumetricLinePosition(nIndex);
        UserObject uo = CreateVolumetricLine(strID, nIndex, pos);

        return uo;
    }

    private UserObject CreateVolumetricLine(string strID, int nIndex, Vector3 pos)
    {
        const float OFFSET_X = 10.0f;
        const float OFFSET_Y = 50.0f;

        UserObject uo = null;

        try
        {
            uo = new UserObject(nIndex);

            uo.m_goName = new GameObject();
            uo.m_goName.transform.localPosition = new Vector3(pos.x + OFFSET_X, pos.y + OFFSET_Y, pos.z);
            uo.m_goName.transform.rotation = Quaternion.Euler(45.0f, 0.0f, 0.0f);

            GameObject volumetricLine = (GameObject)Resources.Load("VolumetricLinePrefab");
            uo.m_goVolumetricLine = UnityEngine.Object.Instantiate(volumetricLine, pos, new Quaternion(0.0f, 90.0f, 90.0f, 1.0f));

            m_dicPenLight.Add(strID, uo);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine(ex.Message);
            System.Diagnostics.Trace.WriteLine(ex.StackTrace);
        }

        return uo;
    }

    private void SetUserName(UserObject uo, string strUserName)
    {
        try
        {
            TextMesh txtName = uo.m_goName.GetComponent<TextMesh>();
            if (txtName == null)
            {
                txtName = uo.m_goName.AddComponent<TextMesh>();
                txtName.characterSize = 1;
                txtName.fontSize = 150;
            }
            txtName.text = strUserName;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine(ex.Message);
            System.Diagnostics.Trace.WriteLine(ex.StackTrace);
        }
    }

    private void SetColor(UserObject uo, Color clrLine)
    {
        try
        {
            VolumetricLineBehavior volLine = uo.m_goVolumetricLine.GetComponent<VolumetricLineBehavior>();
            volLine.LineColor = clrLine;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine(ex.Message);
            System.Diagnostics.Trace.WriteLine(ex.StackTrace);
        }
    }

    private void CreatePopup(UserObject uo, string strMessage)
    {
        try
        {
            GameObject goCanvas = GameObject.Find("Main Canvas");

            GameObject popup = (GameObject)Resources.Load("MyPopup");
            uo.m_goPopup = UnityEngine.Object.Instantiate(popup);
            uo.m_goPopup.transform.SetParent(goCanvas.transform, false);
            uo.m_goPopup.transform.localScale.Normalize();

            Button btn = uo.m_goPopup.GetComponent<Button>();
            GameObject goText = btn.transform.Find("Text").gameObject;

            Text txt = goText.GetComponent<Text>();
            txt.text = strMessage;

            UpdatePopup(uo);

            UnityEngine.Object.Destroy(uo.m_goPopup, NM_POPUP_TIMEOUT);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine(ex.Message);
            System.Diagnostics.Trace.WriteLine(ex.StackTrace);
        }
    }

    private void UpdateVolumetricLine(UserObject uo)
    {
        try
        {
            float fCurrentAngle = Mathf.Round(uo.m_goVolumetricLine.transform.localEulerAngles.x);
            if (fCurrentAngle >= 180)
                fCurrentAngle = fCurrentAngle - 180;

            switch (uo.m_vlType)
            {
                case UserObject.VolumetricLineType.Back:
                    {
                        if (fCurrentAngle < 170)
                        {
                            uo.m_goVolumetricLine.transform.Rotate(new Vector3(-NM_LINE_SPEED, 0f, 0f));
                        }
                        else
                        {
                            uo.m_vlType = UserObject.VolumetricLineType.Front1;

                            if (m_bDemo)
                            {
                                CreatePopup(uo, "おい！");
                            }

                            uo.m_goVolumetricLine.transform.Rotate(new Vector3(NM_LINE_SPEED, 0f, 0f));
                        }
                    }
                    break;
                case UserObject.VolumetricLineType.Front1:
                    {
                        uo.m_goVolumetricLine.transform.Rotate(new Vector3(NM_LINE_SPEED, 0f, 0f));
                        if (uo.m_fLastAngle < fCurrentAngle)
                        {
                            // 90度折り返し
                            uo.m_vlType = UserObject.VolumetricLineType.Front2;
                        }
                    }
                    break;
                case UserObject.VolumetricLineType.Front2:
                    {
                        if (fCurrentAngle < 150)
                        {
                            uo.m_goVolumetricLine.transform.Rotate(new Vector3(NM_LINE_SPEED, 0f, 0f));
                        }
                        else
                        {
                            uo.m_vlType = UserObject.VolumetricLineType.Return;

                            uo.m_goVolumetricLine.transform.Rotate(new Vector3(-NM_LINE_SPEED, 0f, 0f));
                        }
                    }
                    break;
                case UserObject.VolumetricLineType.Return:
                    {
                        if (fCurrentAngle < uo.m_fLastAngle)
                        {
                            // 90度到達
                            uo.m_goVolumetricLine.transform.Rotate(new Vector3(-NM_LINE_SPEED, 0f, 0f));
                        }
                        else
                        {
                            uo.m_vlType = UserObject.VolumetricLineType.Stop;
                        }
                    }
                    break;
            }

            uo.m_fLastAngle = fCurrentAngle;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine(ex.Message);
            System.Diagnostics.Trace.WriteLine(ex.StackTrace);
        }
    }

    private void UpdatePopup(UserObject uo)
    {
        try
        {
            if (uo.m_goPopup == null)
                return;

            GameObject goCanvas = GameObject.Find("Main Canvas");

            RectTransform rectCanvas = goCanvas.GetComponent<RectTransform>();
            RectTransform rectPopup = uo.m_goPopup.GetComponent<RectTransform>();

            Vector2 posPopup;
            Vector2 posScreen = RectTransformUtility.WorldToScreenPoint(Camera.main, uo.m_goName.transform.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectCanvas, posScreen, Camera.main, out posPopup);

            posPopup.y = posPopup.y + NM_POPUP_POS_Y;
            rectPopup.localPosition = posPopup;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine(ex.Message);
            System.Diagnostics.Trace.WriteLine(ex.StackTrace);
        }
    }
}
