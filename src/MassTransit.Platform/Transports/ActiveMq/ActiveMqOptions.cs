namespace MassTransit.Platform.Transports.ActiveMq
{
    public class ActiveMqOptions
    {
        public ActiveMqOptions()
        {
            Host = MassTransitHost.IsInDocker ? "activemq" : "localhost";
            Port = 61616;
            User = "admin";
            Pass = "admin";
        }

        public string Host { get; set; }
        public ushort Port { get; set; }
        public bool Ssl { get; set; }
        public string User { get; set; }
        public string Pass { get; set; }
    }
}
