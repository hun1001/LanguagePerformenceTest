using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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
    private Dictionary<string, Stopwatch> _stopwatch;

    #region Logic

    private void Awake()
    {
        foreach( ServerType serverType in Enum.GetValues( typeof( ServerType ) ) )
        {
            _clientsDictionary.Add( serverType, new TcpChatClient[ _clientCount ] );
            if( serverType.Equals( ServerType.CSMemoryPack ) )
            {
                InitClients<TcpChatMemoryPackClient>( serverType );
            }
            else
            {
                InitClients<TcpChatClient>( serverType );
            }
        }

        _performancePanels = new Dictionary<ServerType, PerformancePanel>();
    }

    private void Start()
    {
        SetPerformancePanel();

        foreach(var cl in _clientsDictionary)
        {
            
        }
    }

    private void Update()
    {
        
    }

    private void FixedUpdate()
    {
        
    }

    #endregion

    #region Methods

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

    private IEnumerator ClientSender(ServerType serverType, string userID)
    {
        for(int i = 0;i<_aClientSendCount;++i )
        {
            
            yield return null;
        }
    }

    private IEnumerator ClientMemorySender(string userID)
    {
        yield return null;
    }

    private IEnumerator Send( ServerType serverType, Packet packet )
    {
        for( int j = 0; j < _aClientSendCount; ++j )
        {
            for( int i = 0; i < _clientCount; ++i )
            {
                _clientsDictionary[ serverType ][ i ].Send( packet );
                yield return null;
            }
        }
    }

    private IEnumerator Send( MemoryPackPacket packet )
    {
        for( int j = 0; j < _aClientSendCount; ++j )
        {
            for( int i = 0; i < _clientCount; ++i )
            {
                _memoryPackClients[ i ].Send( packet );
                yield return null;
            }
        }
    }

    #endregion
}
