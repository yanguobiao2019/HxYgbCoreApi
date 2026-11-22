using Application.Services;
using HxYgbCoreWebApi.Models;
using Infrastructure.AI;
using Infrastructure.Data;
using Infrastructure.Interfaces;
using Infrastructure.Redis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using ScaffoldData.Context;
using ScaffoldData.Models;
using Serilog;
using Serilog.Context;
using Serilog.Core;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace HxYgbCoreWebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    //[Authorize(Roles = "Admin1")]
    //[ProducesResponseType(typeof(ResultModel<List<WeatherForecast>>), 200)]
    public class WeatherForecastController : ControllerBase
    {
        private readonly ILogger<WeatherForecastController> _logger;
        private readonly IRabbitMQService _rabbitMQService;
        private readonly RedisStringService _redisStringService;
        private readonly ChatService _chatService;
        private readonly SerilogDemoContext _serilogDemoContext;
        public WeatherForecastController(ILogger<WeatherForecastController> logger, IRabbitMQService rabbitMQService, 
            RedisStringService redisStringService, ChatService chatService, SerilogDemoContext serilogDemoContext)
        {
            _logger= logger;
            _rabbitMQService = rabbitMQService;
            _redisStringService = redisStringService;
            _chatService = chatService;
            _serilogDemoContext = serilogDemoContext;
        }
        private static readonly string[] Summaries =
        [
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        ];
        //[HttpGet(Name = "GetWeatherForecast")]
        //public async Task<IEnumerable<WeatherForecast>> Get()
        //{
        //    // HxykContext hxykContext = new HxykContext();
        //    // var empList= hxykContext.TSalaryDisclosures.Where(x => x.EmpName=="严国彪").ToList();

        //    //WeatherForecast weatherForecast = new WeatherForecast() {
        //    //    Date = DateOnly.FromDateTime(DateTime.Now.AddDays(1)),
        //    //    TemperatureC = Random.Shared.Next(-20, 55),
        //    //    Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        //    //};
        //    //var data= Enumerable.Range(1, 5).Select(index => new WeatherForecast
        //    //{
        //    //    Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
        //    //    TemperatureC = Random.Shared.Next(-20, 55),
        //    //    Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        //    //})
        //    //.ToList();
        //    //string currentUserId = "User_Alice_88";
        //    //string currentRequestId = Guid.NewGuid().ToString().Substring(0, 8);
        //    //using (LogContext.PushProperty("MachineName", Environment.MachineName))
        //    //using (LogContext.PushProperty("UserName", currentUserId))
        //    //using (LogContext.PushProperty("RequestId", currentRequestId))
        //    //{
        //    //    _logger.LogWarning("2. 嵌套作用域：当前用户 {UserName} 的操作，请求ID {RequestId}", currentUserId, currentRequestId);
        //    //    // 即使不显式在消息模板中写 {UserName}，只要 LogContext 里有，且列名匹配，就会存入数据库
        //    //    _logger.LogWarning("{@data}", data);
        //    //}
        //    //using (var rabbitMQConnection = await _rabbitMQService.CreateConnection()) {
        //    //   var channel = await  rabbitMQConnection.CreateChannelAsync();
        //    //    await channel.ExchangeDeclareAsync(exchange: "logs", type: "direct", durable: true,autoDelete: false);
        //    //    string message = $"Hello World! {DateTime.Now}";//消息内容
        //    //    byte[] body = System.Text.Encoding.UTF8.GetBytes(message);
        //    //    var properties = new BasicProperties();//创建消息属性
        //    //    properties.Persistent = true;//消息持久化
        //    //    await channel.BasicPublishAsync(exchange: "logs",
        //    //                           routingKey: "my_log",
        //    //                           mandatory: true,//强制投递
        //    //                           basicProperties: properties,// 消息属性
        //    //                           body: body);//发布消息
        //    //}
        //        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
        //        {
        //            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
        //            TemperatureC = Random.Shared.Next(-20, 55),
        //            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        //        })
        //        .ToArray();
        //}
        [HttpGet(Name = "GetOrder")]
        public async Task<string> Get(string orderId) {
            _serilogDemoContext.Users.Add(new ScaffoldData.Models.SerilogDemo.User
            {
                Id = 1,
                Name = "Alice",
                Account = "alice123",
                Pwd = "12345"
            });
            _serilogDemoContext.SaveChanges();
            var response= _chatService.SendMessageAsync(orderId);
            return await response;
        }
    }
}
