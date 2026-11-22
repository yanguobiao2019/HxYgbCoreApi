using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Interfaces
{
    public interface IRabbitMQService
    {
       Task<IConnection> CreateConnection();
    }
}
