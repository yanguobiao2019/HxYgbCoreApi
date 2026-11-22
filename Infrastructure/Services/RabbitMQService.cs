using Infrastructure.Configuration;
using Infrastructure.Interfaces;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class RabbitMQService : IRabbitMQService
    {
        private  readonly RabbitMqConfig _rabbitMqConfig=new RabbitMqConfig();

        public RabbitMQService(RabbitMqConfig rabbitMqConfig)
        {
            _rabbitMqConfig = rabbitMqConfig;
        }

        public async Task<IConnection> CreateConnection()
        {
            var factory = new ConnectionFactory
            {
                HostName = _rabbitMqConfig.HostName,
                Port = int.Parse(_rabbitMqConfig.Port),
                UserName = _rabbitMqConfig.UserName,
                Password = _rabbitMqConfig.Password
            };

            return await factory.CreateConnectionAsync();
        }
    }
}
