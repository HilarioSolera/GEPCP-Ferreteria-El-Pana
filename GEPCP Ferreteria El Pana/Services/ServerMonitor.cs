namespace GEPCP_Ferreteria_El_Pana.Services
{
    /// <summary>
    /// Monitor para rastrear los pings del cliente y detectar desconexiones
    /// </summary>
    public static class ServerMonitor
    {
        public static DateTime LastPingTime { get; set; } = DateTime.UtcNow;
        public static bool HasReceivedPing { get; set; } = false;

        public static void UpdateLastPing()
        {
            LastPingTime = DateTime.UtcNow;
            HasReceivedPing = true;
        }

        public static bool IsClientConnected(int timeoutMilliseconds = 10000)
        {
            if (!HasReceivedPing)
                return true;

            return (DateTime.UtcNow - LastPingTime).TotalMilliseconds <= timeoutMilliseconds;
        }
    }
}
