using MemoryPack;
using System;
using System.Text;

[MemoryPackable]
public partial class MemoryPackPacket
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
        var packetData = $"{UserID}|{TimeStamp}|{Message}";

        var bin = Encoding.UTF8.GetBytes(packetData);

        return bin;
    }

    public static Packet Deserialize(byte[] bin)
    {
        var packetData = Encoding.UTF8.GetString(bin);

        var splitData = packetData.Split('|');

        var userID = splitData[0];
        var timeStamp = splitData[1];
        var message = splitData[2];

        return new Packet(userID, timeStamp, message);
    }
}