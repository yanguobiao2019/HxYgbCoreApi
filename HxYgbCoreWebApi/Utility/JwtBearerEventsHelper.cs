using HxYgbCoreWebApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace HxYgbCoreWebApi.Utility
{
    public class JwtBearerEventsHelper
    {
        public static Task AuthenticationFailedHandler(AuthenticationFailedContext context) {
            throw new ExceptionExtData("✗ JWT AuthenticationFailed", ExceptionExtEnum.BadRequest);
        }
        public static Task TokenValidatedHandler(TokenValidatedContext context)
        {
            Console.WriteLine("✗ JWT 认证 TokenValidatedHandler");
            return Task.CompletedTask;
        }
        public static Task MessageReceivedHandler(MessageReceivedContext context)
        {
            Console.WriteLine("✗ JWT 认证MessageReceivedHandler");
            context.Request.Headers.TryGetValue("Authorization", out var authorizationHeader);
            Console.WriteLine(authorizationHeader);
            return Task.CompletedTask;
        }
        public static Task ChallengeHandler(JwtBearerChallengeContext context)
        {
            throw new ExceptionExtData("✗ JWT 认证ChallengeHandler", ExceptionExtEnum.Unauthorized);
            //return Task.CompletedTask;
        }
        public static Task ForbiddenHandler(ForbiddenContext context)
        {
            throw new ExceptionExtData("✗ JWT Forbidden", ExceptionExtEnum.Forbidden);
        }
    }
}
