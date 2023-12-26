using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PerformanceManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int _clientCount = 100;
    [SerializeField] private int _aClientSendCount = 10;

    private Dictionary<ServerType, TcpChatClient[]> _clients;
    private TcpChatMemoryPackClient[] _memoryPackClients => _clients[ServerType.CSMemoryPack] as TcpChatMemoryPackClient[];

    #region Logic

    private void Awake()
    {
        foreach (ServerType serverType in System.Enum.GetValues(typeof(ServerType)))
        {
            _clients.Add(serverType, new TcpChatClient[_clientCount]);
            if(serverType.Equals(ServerType.CSMemoryPack))
            {
                InitClients<TcpChatMemoryPackClient>(serverType);
            }
            else
            {
                InitClients<TcpChatClient>(serverType);
            }
        }
    }

    private void Start()
    {

    }

    private void Update()
    {
        
    }

    private void FixedUpdate()
    {
        
    }

    #endregion

    #region Methods

    private void InitClients<T>(ServerType serverType) where T : TcpChatClient, new()
    {
        for (int i = 0; i < _clientCount; ++i)
        {
            _clients[ serverType ][i] = new T();
            _clients[ serverType ][i].Init(serverType);
        }
    }

    private IEnumerator SendPacket(ServerType serverType, Packet packet)
    {
        for (int i = 0; i < _clientCount; ++i)
        {
            _clients[ serverType ][i].Send(packet);
            yield return null;
        }
    }

    private IEnumerator SendPacket(ServerType serverType, MsgPackPacket packet)
    {
        for (int i = 0; i < _clientCount; ++i)
        {
            _memoryPackClients[i]?.Send(packet);
            yield return null;
        }
    }

    #endregion
}
