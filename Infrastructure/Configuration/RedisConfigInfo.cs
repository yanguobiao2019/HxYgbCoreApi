using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Configuration
{
    public sealed class RedisConfigInfo
    {
        /// <summary>
        /// 可写的Redis链接地址
        /// format:ip1,ip2
        /// 默认6379端口
        ///
        /// 如果有密码按下面格式填写
        /// password@ip:port
        /// </summary>
        public string WriteServerList = "@mosyan850415@1.13.19.178:6379";
        /// <summary>
        /// 可读的Redis链接地址
        /// format:ip1,ip2
        /// </summary>
        public string ReadServerList = "@mosyan850415@1.13.19.178:6379";
        /// <summary>
        /// 最大写链接数
        /// </summary>
        public int MaxWritePoolSize = 60;
        /// <summary>
        /// 最大读链接数
        /// </summary>
        public int MaxReadPoolSize = 60;
        /// <summary>
        /// 本地缓存到期时间，单位:秒
        /// </summary>
        public int LocalCacheTime = 180;
        /// <summary>
        /// 自动重启
        /// </summary>
        public bool AutoStart = true;
        /// <summary>
        /// 是否记录日志,该设置仅用于排查redis运行时出现的问题,
        /// 如redis工作正常,请关闭该项
        /// </summary>
        public bool RecordeLog = false;
    }
}
