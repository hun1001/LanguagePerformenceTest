using UnityEngine;
using UnityEngine.UI;

public class ChatManager : MonoBehaviour
{
    [SerializeField] private InputField userIdInputField;

    [SerializeField] private InputField messageInputField;

    [SerializeField] private Transform messageListTransform;

    [SerializeField] private Scrollbar scrollbar;

    [SerializeField] private ChatObject chatObject;

    [SerializeField] private Clock clock;

    [SerializeField] private ServerType type = ServerType.Cpp;

    private TcpChatClient _tcpChatClient;
    private TcpChatMemoryPackClient _tcpChatMemoryPackClient => _tcpChatClient as TcpChatMemoryPackClient;

    private void Awake() => _tcpChatClient = type == ServerType.CSMemoryPack ? new TcpChatMemoryPackClient( type ) : new TcpChatClient( type );

    private void Start()
    {
        if( type == ServerType.CSMemoryPack )
        {
            _tcpChatMemoryPackClient?.AddReceivePacketListener( OnReceiveMemoryPackMessage );
            StartCoroutine( _tcpChatMemoryPackClient?.Receive() );
        }
        else
        {
            _tcpChatClient.AddReceivePacketListener( OnReceivePacketMessage );
            StartCoroutine( _tcpChatClient.Receive() );
        }
    }

    private void Update()
    {
        if( Input.GetKeyDown( KeyCode.Return ) )
        {
            MessageSend();
        }
    }

    public void MessageSend()
    {
        if( type == ServerType.CSMemoryPack )
        {
            SendMessageMsgPack();
        }
        else
        {
            SendMessageCustom();
        }
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

        _tcpChatMemoryPackClient?.Send(packet);

        messageInputField.text = "";
    }

    private void SendMessageCustom()
    {
        if (string.IsNullOrEmpty(userIdInputField.text) || string.IsNullOrEmpty(messageInputField.text)) return;

        var packet = new Packet(userIdInputField.text, clock.GetTime(), messageInputField.text);

        _tcpChatClient.Send(packet);

        messageInputField.text = "";
    }

    private void OnReceivePacketMessage(Packet p)
    {
        var component = Instantiate(chatObject, messageListTransform).GetComponent<ChatObject>();
        component.SetText(p);

        scrollbar.value = 0;

        if (messageListTransform.childCount > 1000) Destroy(messageListTransform.GetChild(0).gameObject);
    }

    private void OnReceiveMemoryPackMessage(MsgPackPacket packet)
    {
        var component = Instantiate(chatObject, messageListTransform).GetComponent<ChatObject>();
        component.SetText(packet);

        scrollbar.value = 0;

        if (messageListTransform.childCount > 1000) Destroy(messageListTransform.GetChild(0).gameObject);
    }
}