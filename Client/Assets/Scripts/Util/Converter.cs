using System.Collections;
using System.Collections.Generic;
using UnityEngine;

static public class Converter
{
    static public string GetLanguage( ServerType serverType ) => serverType switch
    {
        ServerType.CSCustom => "C#",
        ServerType.CSMemoryPack => "C# MemoryPack",
        ServerType.Cpp => "C++",
        ServerType.Rust => "Rust",
        _ => "Unknown"
    };

    static public string Dec2Hex( int dec ) => dec.ToString( "X" );
}
