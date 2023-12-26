using System.Collections;
using System.Collections.Generic;
using UnityEngine;

static public class MessageGenerator
{
    static public int messageCount = 0;

    static public string newMessage()
    {
        return Converter.Dec2Hex(messageCount++);
    }
}
