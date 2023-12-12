using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MemoryPack;

namespace MemoryPackChatServer
{
    [MemoryPackable]
    public partial class Packet
    {
        public string UserID { get; set; }
        public string TimeStamp { get; set; }
        public string Message { get; set; }

        public Packet()
        {
            UserID = "";
            TimeStamp = "";
            Message = "";
        }
    }
}

namespace CustomPacket
{
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

        public static Packet Deserialize(byte[] buffer)
        {
            var packet = new Packet();

            var str = Encoding.UTF8.GetString(buffer);
            var split = str.Split('|');

            packet.UserID = split[0];
            packet.TimeStamp = split[1];
            packet.Message = split[2];

            return packet;
        }

        public byte[] Serialize()
        {
            var str = $"{UserID}|{TimeStamp}|{Message}";
            return Encoding.UTF8.GetBytes(str);
        }
    }
}