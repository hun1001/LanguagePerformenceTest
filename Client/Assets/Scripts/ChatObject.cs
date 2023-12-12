using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChatObject : MonoBehaviour
{
    [SerializeField]
    private Text _chatText = null;

    public void SetText(MsgPackPacket packet)
    {
        _chatText.text = $"[{packet.TimeStamp}] {packet.UserID}: {packet.Message}";
    }

    public void SetText(Packet packet)
    {
        _chatText.text = $"[{packet.TimeStamp}] {packet.UserID}: {packet.Message}";
    }
}
