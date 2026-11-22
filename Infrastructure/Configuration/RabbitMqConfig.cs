using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Configuration
{
    public class RabbitMqConfig
    {
        public string HostName { get; set; } = "localhost";
        public string Port { get; set; } = "5672";
        public string UserName { get; set; } = "guest";
        public string Password { get; set; }= "guest"; 
    }
}
