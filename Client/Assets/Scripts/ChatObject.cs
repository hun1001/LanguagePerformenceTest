using UnityEngine;
using UnityEngine.UI;

public class ChatObject : MonoBehaviour
{
    [SerializeField] private Text chatText;

    public void SetText(MsgPackPacket packet)
    {
        chatText.text = $"[{packet.TimeStamp}] {packet.UserID}: {packet.Message}";
    }

    public void SetText(Packet packet)
    {
        chatText.text = $"[{packet.TimeStamp}] {packet.UserID}: {packet.Message}";
    }
}