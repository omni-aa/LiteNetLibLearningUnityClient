using System;


public static class ClientEvents
{
    public static event Action<string> OnMOTD;

    public static void RaiseMOTD(string message)
    {
        OnMOTD?.Invoke(message);
    }
}