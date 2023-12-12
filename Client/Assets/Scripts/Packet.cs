using MemoryPack;
using System;
using UnityEngine;
using System.Text;

[MemoryPackable]
public partial class MsgPackPacket
{
    public string UserID { get; set; }
    public string TimeStamp { get; set; }
    public string Message { get; set; }
}

[Serializable]
public struct Packet
{
    public string UserID { get; set; }
    public string TimeStamp { get; set; }
    public string Message { get; set; }

    public Packet(string userID, string timeStamp, string message)
    {
        UserID = userID;
        TimeStamp = timeStamp;
        Message = message;
    }

    public byte[] Serialize()
    {
        string packetData = $"{UserID}|{TimeStamp}|{Message}";

        var bin = Encoding.UTF8.GetBytes(packetData);

        return bin;
    }

    public static Packet Deserialize(byte[] bin)
    {
        string packetData = Encoding.UTF8.GetString(bin);

        string[] splitData = packetData.Split('|');

        string userID = splitData[0];
        string timeStamp = splitData[1];
        string message = splitData[2];

        return new Packet(userID, timeStamp, message);
    }
}