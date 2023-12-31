using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class PerformanceManager : MonoBehaviour
{
    [Header("Settings")]
    [Range(1, 1000)]
    [SerializeField] private int _clientCount = 100;
    [SerializeField] private int _aClientSendCount = 10;

    [Space(10)]

    [SerializeField] private float _clientSendDelay = 0f;

    [Header("UI")] [SerializeField] private Transform _contentTransform;
    [SerializeField] private PerformancePanel _performancePrefab;

    private Dictionary<ServerType, TcpChatClient[]> _clientsDictionary;

    private TcpChatMemoryPackClient GetMemoryPackClient(int i) => _clientsDictionary[ ServerType.CSMemoryPack ][i] as TcpChatMemoryPackClient;

    private Dictionary<ServerType, PerformancePanel> _performancePanels;
    private Dictionary<string, ServerWatch> _serverWatchDictionary;

    private WaitForSeconds _clientSendDelaySeconds;

    #region Logic

    private void Awake()
    {
        _clientsDictionary = new Dictionary<ServerType, TcpChatClient[]>();
        _performancePanels = new Dictionary<ServerType, PerformancePanel>();
        _serverWatchDictionary = new Dictionary<string, ServerWatch>();

        _clientSendDelaySeconds = new WaitForSeconds(_clientSendDelay);

        foreach (ServerType serverType in Enum.GetValues(typeof(ServerType)))
        {
            _clientsDictionary.Add(serverType, new TcpChatClient[_clientCount]);
            if (serverType.Equals(ServerType.CSMemoryPack))
            {
                InitClients<TcpChatMemoryPackClient>( serverType );
            }
            else
            {
                InitClients<TcpChatClient>(serverType);
            }
        }
    }

    private void Start()
    {
        SetBaseClient();
        SetPerformancePanel();

        foreach (var cl in _clientsDictionary)
        {
            if(TcpChatClient.IsFailedServer(cl.Key))
            {
                continue;
            }

            SendStart(cl.Key);
        }
    }

    #endregion

    #region Methods

    public void SendStart( ServerType serverType ) => StartCoroutine( SendStartCoroutine( serverType ) );

    private IEnumerator SendStartCoroutine(ServerType serverType)
    {
        for( int j = 0; j < _aClientSendCount; ++j )
        {
            if( serverType.Equals( ServerType.CSMemoryPack ) )
            {
                for( int i = 0; i < _clientCount; ++i )
                {
                    ClientMemorySender( $"{Converter.GetLanguage( serverType )}_Test{i}", i );
                    yield return _clientSendDelaySeconds;
                }
            }
            else
            {
                for( var i = 0; i < _clientCount; ++i )
                {
                    ClientSender( serverType, $"{Converter.GetLanguage( serverType )}_Test{i}", i );
                    yield return _clientSendDelaySeconds;
                }
            }
        }
    }

    private void SetBaseClient()
    {
        foreach (var clients in _clientsDictionary)
        {
            if(TcpChatClient.IsFailedServer(clients.Key))
            {
                continue;
            }

            if (clients.Key.Equals(ServerType.CSMemoryPack))
            {
                GetMemoryPackClient(0).AddReceivePacketListener( ReceiveMemoryPacket );
                StartCoroutine( GetMemoryPackClient( 0 ).Receive());
            }
            else
            {
                clients.Value[0].AddReceivePacketListener(ReceivePacket);
                StartCoroutine( clients.Value[0].Receive());
            }
        }
    }

    private void SetPerformancePanel()
    {
        foreach (var clients in _clientsDictionary)
        {
            if(TcpChatClient.IsFailedServer(clients.Key))
            {
                continue;
            }

            var panel = Instantiate(_performancePrefab, _contentTransform).GetComponent<PerformancePanel>();

            panel.Init(Converter.GetLanguage(clients.Key));

            _performancePanels.Add(clients.Key, panel);
        }
    }

    private void InitClients<T>(ServerType serverType) where T : TcpChatClient, new()
    {
        for (var i = 0; i < _clientCount; ++i)
        {
            _clientsDictionary[serverType][i] = new T();
            _clientsDictionary[serverType][i].Init(serverType);
        }
    }

    private void ClientSender(ServerType serverType, string userID, int index)
    {
        var msg = MessageGenerator.newMessage();
        var packet = new Packet(userID, DateTime.Now.ToString("T"), msg);

        _serverWatchDictionary.Add(msg, new ServerWatch(serverType));

        _serverWatchDictionary[msg].Start();
        _clientsDictionary[serverType][index].Send(packet);    
    }

    private void ClientMemorySender(string userID, int index)
    {
        var msg = MessageGenerator.newMessage();
        var packet = new MemoryPackPacket
        {
            UserID = userID,
            TimeStamp = DateTime.Now.ToString("T"),
            Message = msg
        };

        _serverWatchDictionary.Add( msg, new ServerWatch( ServerType.CSMemoryPack ) );

        _serverWatchDictionary[ msg ].Start();
        GetMemoryPackClient( index ).Send( packet );
    }

    private void ReceivePacket(Packet packet)
    {
        if( _serverWatchDictionary.ContainsKey( packet.Message) )
        {
            var watch = _serverWatchDictionary[packet.Message];
            watch.Stop();

            _performancePanels[ watch.ServerType ].Add( watch.GetMicroseconds() );
        }
        else
        {
            Debug.LogError( $"Not Found Key {packet.Message}" );
        }
    }

    private void ReceiveMemoryPacket(MemoryPackPacket packet)
    {
        if( _serverWatchDictionary.ContainsKey( packet.Message ) )
        {
            var watch = _serverWatchDictionary[packet.Message];
            watch.Stop();

            _performancePanels[ watch.ServerType ].Add( watch.GetMicroseconds() );
        }
        else
        {
            Debug.LogError( $"Not Found Key {packet.Message}" );
        }
    }
    #endregion
}