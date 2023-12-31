using MemoryPack;
using MessagePack;
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

        message = message.Substring(0, message.IndexOf('\0'));

        return new Packet(userID, timeStamp, message);
    }

    public override string ToString()
    {
        return $"UserID: {UserID}, TimeStamp: {TimeStamp}, Message: {Message}";
    }
}

[MessagePackObject]
public class MsgPackPacket
{
    [Key(0)]
    public string UserID { get; set; }

    [Key(1)]
    public string TimeStamp { get; set; }

    [Key(2)]
    public string Message { get; set; }
}