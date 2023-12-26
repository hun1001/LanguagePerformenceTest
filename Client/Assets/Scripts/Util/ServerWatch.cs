using System.Diagnostics;

public class ServerWatch : Stopwatch
{
    public readonly ServerType ServerType;

    public ServerWatch(ServerType serverType)
    {
        ServerType = serverType;
    }
}
