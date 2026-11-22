namespace HxYgbCoreWebApi.Models
{
    public class ResultModel<T>
    {
        /// <summary>
        /// 返回结果编码 0：失败 1：成功
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// 返回结果内容 成功：Success  失败：异常内容
        /// </summary>
        public string Msg { get; set; } = string.Empty;

        /// <summary>
        /// 返回结果 成功：返回T类型数据 失败：默认null
        /// </summary>
        public T? Data { get; set; }
        /// <summary>
        /// 执行时长
        /// </summary>
        public virtual long TimeOut { get; set; }
    }
    public class ResultModel
    {
        /// <summary>
        /// 返回结果编码 0：失败 1：成功
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// 返回结果内容 成功：Success  失败：异常内容
        /// </summary>
        public string Msg { get; set; } = string.Empty;

        /// <summary>
        /// 返回结果 成功：返回T类型数据 失败：默认null
        /// </summary>
        public object Data { get; set; } 
        /// <summary>
        /// 执行时长
        /// </summary>
        public virtual long TimeOut { get; set; }
    }
}
