using HxYgbCoreWebApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace HxYgbCoreWebApi.Middleware
{
    public class CustomerExceptionMiddleware
    {
        /// <summary>
        /// 委托
        /// </summary>
        private readonly RequestDelegate _next;
        public CustomerExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }
        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (ExceptionExtData ex)
            {
               await ExceptionMsgHandler(context,ex);
            }
        }
        public async Task ExceptionMsgHandler(HttpContext context,ExceptionExtData ex) {
            context.Response.ContentType = "application/problem+json";
            string msg = "";
            switch (ex.Type)
            {
                case ExceptionExtEnum.BadRequest:
                    msg = "Jwt 授权失败";
                    break;
                case ExceptionExtEnum.Unauthorized:
                    msg = "Jwt 未授权";
                    break;
                case ExceptionExtEnum.Forbidden:
                    msg = "Jwt 访问被拒绝";
                    break;
                case ExceptionExtEnum.NotFound:
                    msg = "Jwt NotFound";
                    break;
                case ExceptionExtEnum.InternalServerError:
                    msg = "Internal Server Error";
                    break;
                default:
                    msg = "An error occurred";
                    break;
            }
            var details = ex.ToString();
            ResultModel<string> result = new ResultModel<string>
            {
                Status = 1,
                Msg = msg,
                Data = { }
            };
            var stream = context.Response.Body;
            await JsonSerializer.SerializeAsync(stream, result);
        }
    }
    public static class ExceptionMiddlewareExtension
    {
        /// <summary>
        /// 静态方法
        /// </summary>
        /// <param name="app">要进行扩展的类型</param>
        public static void UseExceptionMiddleware(this IApplicationBuilder app)
        {
            app.UseMiddleware(typeof(CustomerExceptionMiddleware));
        }
    }
}
