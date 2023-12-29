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
