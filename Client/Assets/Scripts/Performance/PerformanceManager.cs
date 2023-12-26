using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PerformanceManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int _clientCount = 100;
    [SerializeField] private int _aClientSendCount = 10;

    [Header("UI")]
    [SerializeField] private Transform _contentTransform;
    [SerializeField] private PerformancePanel _performancePrefab;

    private Dictionary<ServerType, TcpChatClient[]> _clientsDictionary;
    private TcpChatMemoryPackClient[] _memoryPackClients => _clientsDictionary[ServerType.CSMemoryPack] as TcpChatMemoryPackClient[];

    private Dictionary<ServerType, PerformancePanel> _performancePanels;
    private Dictionary<string, ServerWatch> _serverwatchDictionary;

    #region Logic

    private void Awake()
    {
        _clientsDictionary = new Dictionary<ServerType, TcpChatClient[]>();
        _performancePanels = new Dictionary<ServerType, PerformancePanel>();
        _serverwatchDictionary = new Dictionary<string, ServerWatch>();

        foreach( ServerType serverType in Enum.GetValues( typeof( ServerType ) ) )
        {
            _clientsDictionary.Add( serverType, new TcpChatClient[ _clientCount ] );
            if( serverType.Equals( ServerType.CSMemoryPack ) )
            {
                //InitClients<TcpChatMemoryPackClient>( serverType );
            }
            else
            {
                InitClients<TcpChatClient>( serverType );
            }
        }
    }

    private void Start()
    {
        SetBaseClient();
        SetPerformancePanel();

        foreach(var cl in _clientsDictionary)
        {
            if(cl.Key.Equals(ServerType.CSMemoryPack))
            {
                for(int i = 0; i<_clientCount; ++i)
                {
                    //StartCoroutine( ClientMemorySender( $"Test{i}", i ) );
                }
            }
            else
            {
                for(int i = 0;i<_clientCount; ++i)
                {
                    StartCoroutine( ClientSender( cl.Key, $"Test{i}", i ) );
                }
            }
        }
    }

    #endregion

    #region Methods

    private void SetBaseClient()
    {
        foreach(var clients in _clientsDictionary)
        {
            if(clients.Key.Equals( ServerType.CSMemoryPack ) )
            {
                //(clients.Value[ 0 ] as TcpChatMemoryPackClient).AddReceivePacketListener( ReceiveMemoryPacket );
            }
            else
            {
                clients.Value[ 0 ].AddReceivePacketListener( ReceivePacket );
            }
        }
    }

    private void SetPerformancePanel()
    {
        foreach(var clients in _clientsDictionary)
        {
            var panel = Instantiate(_performancePrefab, _contentTransform).GetComponent<PerformancePanel>();

            panel.Init( Converter.GetLanguage( clients.Key ) );

            _performancePanels.Add(clients.Key, panel);
        }
    }

    private void InitClients<T>(ServerType serverType) where T : TcpChatClient, new()
    {
        for (int i = 0; i < _clientCount; ++i)
        {
            _clientsDictionary[ serverType ][ i ] = new T();
            _clientsDictionary[ serverType ][ i ].Init( serverType );
        }
    }

    private IEnumerator ClientSender(ServerType serverType, string userID, int index)
    {
        for(int i = 0;i<_aClientSendCount;++i )
        {
            string msg = MessageGenerator.newMessage();
            Packet packet = new Packet(userID, DateTime.Now.ToString("T"), msg);

            _serverwatchDictionary.Add(msg, new ServerWatch(serverType));

            _serverwatchDictionary[msg].Start();
            _clientsDictionary[ serverType ][ index ].Send( packet );

            yield return null;
        }
    }

    private IEnumerator ClientMemorySender(string userID, int index)
    {
        for( int i = 0; i<_aClientSendCount; ++i )
        {
            string msg = MessageGenerator.newMessage();
            Packet packet = new Packet(userID, DateTime.Now.ToString("T"), msg);

            _serverwatchDictionary.Add( msg, new ServerWatch( ServerType.CSMemoryPack ) );

            _serverwatchDictionary[ msg ].Start();
            _clientsDictionary[ ServerType.CSMemoryPack ][ index ].Send( packet );

            yield return null;
        }
    }

    private void ReceivePacket(Packet packet)
    {
        Debug.Log( "Recv Packet Data" );

        if( _serverwatchDictionary.ContainsKey( packet.Message ) )
        {
            ServerWatch watch = _serverwatchDictionary[ packet.Message ];
            watch.Stop();

            _performancePanels[ watch.ServerType ].Add( watch.GetMicroseconds() );

        }
        else
        {
            Debug.LogError( "Not Contain Key" );
        }
    }

    private void ReceiveMemoryPacket(MemoryPackPacket packet )
    {
        Debug.Log( "Recv Memory Data" );

        if( _serverwatchDictionary.ContainsKey( packet.Message ) )
        {
            ServerWatch watch = _serverwatchDictionary[ packet.Message ];
            watch.Stop();

            _performancePanels[ watch.ServerType ].Add( watch.GetMicroseconds() );
        }
        else
        {
            Debug.LogError( "Not Contain Key" );
        }
    }

    #endregion
}
