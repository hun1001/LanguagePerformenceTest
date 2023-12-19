using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [SerializeField] private InputField userIdInputField;

    [SerializeField] private InputField messageInputField;

    [SerializeField] private Transform messageListTransform;

    [SerializeField] private Scrollbar scrollbar;

    [SerializeField] private ChatObject chatObject;

    [SerializeField] private Clock clock;

    private TcpChatMsgPackClient _tcpChatClient;
    private TcpChatCustomClient _tcpChatCustomClient;

    private Stopwatch _stopwatch;

    private enum Protocol
    {
        Custom,
        MsgPack
    }

    [SerializeField] private Protocol protocol = Protocol.MsgPack;

    private void Awake()
    {
        _stopwatch = new Stopwatch();

        switch (protocol)
        {
            case Protocol.Custom:
                _tcpChatCustomClient = new TcpChatCustomClient();
                break;
            case Protocol.MsgPack:
                _tcpChatClient = new TcpChatMsgPackClient();
                break;
        }
    }

    private void Start()
    {
        switch (protocol)
        {
            case Protocol.Custom:
                _tcpChatCustomClient.AddReceivePacketListener(OnReceivePacketMessage);
                StartCoroutine(_tcpChatCustomClient.Receive());
                break;
            case Protocol.MsgPack:
                _tcpChatClient.AddReceivePacketListener(OnReceiveMsgPackMessage);
                StartCoroutine(_tcpChatClient.Receive());
                break;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (protocol == Protocol.Custom)
                SendMessageCustom();
            else
                SendMessageMsgPack();
        }
    }

    public void MessageSend()
    {
        if (protocol == Protocol.Custom)
            SendMessageCustom();
        else
            SendMessageMsgPack();
    }

    private void SendMessageMsgPack()
    {
        if (string.IsNullOrEmpty(userIdInputField.text) || string.IsNullOrEmpty(messageInputField.text)) return;

        var packet = new MsgPackPacket
        {
            UserID = userIdInputField.text,
            TimeStamp = clock.GetTime(),
            Message = messageInputField.text
        };

#if UNITY_EDITOR
        _stopwatch.Start();
#endif
        _tcpChatClient.Send(packet);

        messageInputField.text = "";
    }

    private void SendMessageCustom()
    {
        if (string.IsNullOrEmpty(userIdInputField.text) || string.IsNullOrEmpty(messageInputField.text)) return;

        var packet = new Packet(userIdInputField.text, clock.GetTime(), messageInputField.text);

#if UNITY_EDITOR
        _stopwatch.Start();
#endif

        _tcpChatCustomClient.Send(packet);

        messageInputField.text = "";
    }

    private void OnReceivePacketMessage(Packet p)
    {
        var component = Instantiate(chatObject, messageListTransform).GetComponent<ChatObject>();
        component.SetText(p);

        scrollbar.value = 0;

#if UNITY_EDITOR
        //if(p.UserID == _userIdInputField.text)
        {
            _stopwatch.Stop();
            var microseconds = _stopwatch.ElapsedTicks / (Stopwatch.Frequency / (1000L * 1000L));
            _stopwatch.Reset();

            UnityEngine.Debug.Log($"Microsecond Time: {microseconds}");
        }
#endif

        if (messageListTransform.childCount > 1000) Destroy(messageListTransform.GetChild(0).gameObject);
    }

    private void OnReceiveMsgPackMessage(MsgPackPacket packet)
    {
        var component = Instantiate(chatObject, messageListTransform).GetComponent<ChatObject>();
        component.SetText(packet);

        scrollbar.value = 0;

#if UNITY_EDITOR
        //if(packet.UserID == _userIdInputField.text)
        {
            _stopwatch.Stop();
            var microseconds = _stopwatch.ElapsedTicks / (Stopwatch.Frequency / (1000L * 1000L));

            UnityEngine.Debug.Log($"Microsecond Time: {microseconds}");
        }
#endif

        if (messageListTransform.childCount > 1000) Destroy(messageListTransform.GetChild(0).gameObject);
    }
}