using System.Diagnostics;

public class ServerWatch : Stopwatch
{
    public readonly ServerType ServerType;

    public ServerWatch(ServerType serverType)
    {
        ServerType = serverType;
    }

    public long GetMicroseconds()
    {
        return ElapsedTicks / (Frequency / (1000L * 1000L));
    }
}
