using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [SerializeField]
    private InputField _userIdInputField = null;

    [SerializeField]
    private InputField _messageInputField = null;

    [SerializeField]
    private Transform _messageListTransform = null;

    [SerializeField]
    private Scrollbar _scrollbar = null;

    [SerializeField]
    private ChatObject _chatObject = null;

    [SerializeField]
    private Clock _clock = null;

    private TcpChatMsgPackClient _tcpChatClient = null;
    private TcpChatCustomClient _tcpChatCustomClient = null;

    private Stopwatch _stopwatch = null;

    public enum Protocol
    {
        Custom,
        MsgPack,
    }

    [SerializeField]
    private Protocol _protocol = Protocol.MsgPack;

    private void Awake()
    {
        _stopwatch = new Stopwatch();

        switch(_protocol)
        {
            case Protocol.Custom:
                _tcpChatCustomClient = new TcpChatCustomClient();
                break;
            case Protocol.MsgPack:
                _tcpChatClient = new TcpChatMsgPackClient();
                break;
        }
    }

    void Start()
    {
        switch(_protocol)
        {
            case Protocol.Custom:
                _tcpChatCustomClient.AddReceivePacketListener(OnRecievePacketMessage);
                StartCoroutine(_tcpChatCustomClient.Receive());
                break;
            case Protocol.MsgPack:
                _tcpChatClient.AddReceivePacketListener(OnReciveMsgPackMessage);
                StartCoroutine(_tcpChatClient.Receive());
                break;
        }
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Return))
        {
            if(_protocol == Protocol.Custom)
            {
                SendMessageCustom();
            }
            else
            {
                SendMessageMsgPack();
            }
        }
    }

    public void MessageSend()
    {
        if(_protocol == Protocol.Custom)
        {
            SendMessageCustom();
        }
        else
        {
            SendMessageMsgPack();
        }
    }

    private void SendMessageMsgPack()
    {
        if(string.IsNullOrEmpty(_userIdInputField.text) || string.IsNullOrEmpty(_messageInputField.text))
        {
            return;
        }

        MsgPackPacket packet = new MsgPackPacket();
        packet.UserID = _userIdInputField.text;
        packet.TimeStamp = _clock.GetTime();
        packet.Message = _messageInputField.text;

#if UNITY_EDITOR
        _stopwatch.Start();
#endif
        _tcpChatClient.Send(packet);

        _messageInputField.text = "";
    }

    private void SendMessageCustom()
    {
        if (string.IsNullOrEmpty(_userIdInputField.text) || string.IsNullOrEmpty(_messageInputField.text))
        {
            return;
        }

        Packet packet = new Packet(_userIdInputField.text, _clock.GetTime(), _messageInputField.text);

#if UNITY_EDITOR
        _stopwatch.Start();
#endif

        _tcpChatCustomClient.Send(packet);

        _messageInputField.text = "";
    }
    
    private void OnRecievePacketMessage(Packet p)
    {
        var chatObject = Instantiate(_chatObject, _messageListTransform).GetComponent<ChatObject>();
        chatObject.SetText(p);

        _scrollbar.value = 0;

#if UNITY_EDITOR
        //if(p.UserID == _userIdInputField.text)
        {
            _stopwatch.Stop();
            long microseconds = _stopwatch.ElapsedTicks / (Stopwatch.Frequency / (1000L * 1000L));
            _stopwatch.Reset();

            UnityEngine.Debug.Log($"Microsec Time: {microseconds}");
        }
#endif

        if (_messageListTransform.childCount > 1000)
        {
            Destroy(_messageListTransform.GetChild(0).gameObject);
        }
    }

    private void OnReciveMsgPackMessage(MsgPackPacket packet)
    {
        var chatObject = Instantiate(_chatObject, _messageListTransform).GetComponent<ChatObject>();
        chatObject.SetText(packet);

        _scrollbar.value = 0;

#if UNITY_EDITOR
        //if(packet.UserID == _userIdInputField.text)
        {
            _stopwatch.Stop();
            long microseconds = _stopwatch.ElapsedTicks / (Stopwatch.Frequency / (1000L * 1000L));

            UnityEngine.Debug.Log($"Microsec Time: {microseconds}");
        }
#endif

        if(_messageListTransform.childCount > 1000)
        {
            Destroy(_messageListTransform.GetChild(0).gameObject);
        }
    }
}
