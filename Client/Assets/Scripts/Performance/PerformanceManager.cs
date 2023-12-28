using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PerformanceManager : MonoBehaviour
{
    [Header("Settings")] [SerializeField] private int _clientCount = 100;
    [SerializeField] private int _aClientSendCount = 10;

    [Header("UI")] [SerializeField] private Transform _contentTransform;
    [SerializeField] private PerformancePanel _performancePrefab;

    private Dictionary<ServerType, TcpChatClient[]> _clientsDictionary;

    private TcpChatMemoryPackClient[] MemoryPackClients =>
        _clientsDictionary[ServerType.CSMemoryPack] as TcpChatMemoryPackClient[];

    private Dictionary<ServerType, PerformancePanel> _performancePanels;
    private Dictionary<string, ServerWatch> _serverWatchDictionary;

    #region Logic

    private void Awake()
    {
        _clientsDictionary = new Dictionary<ServerType, TcpChatClient[]>();
        _performancePanels = new Dictionary<ServerType, PerformancePanel>();
        _serverWatchDictionary = new Dictionary<string, ServerWatch>();

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

    private IEnumerator Start()
    {
        SetBaseClient();
        SetPerformancePanel();

        foreach (var cl in _clientsDictionary)
        {
            if(TcpChatClient.IsFailedServer(cl.Key))
            {
                continue;
            }

            for( int j = 0; j < _aClientSendCount; ++j )
            {
                if( cl.Key.Equals( ServerType.CSMemoryPack ) )
                {
                    for( int i = 0; i < _clientCount; ++i )
                    {
                        ClientMemorySender( $"{Converter.GetLanguage(cl.Key)}_Test{i}", i );
                    }
                }
                else
                {
                    for( var i = 0; i < _clientCount; ++i )
                    {
                        ClientSender( cl.Key, $"{Converter.GetLanguage( cl.Key )}_Test{i}", i );
                    }
                }
                yield return null;
            }
        }
    }

    #endregion

    #region Methods

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
                MemoryPackClients[0].AddReceivePacketListener( ReceiveMemoryPacket );
                StartCoroutine( MemoryPackClients[0].Receive());
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
        MemoryPackClients[ index ].Send( packet );
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