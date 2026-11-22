using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Infrastructure.AI
{
    public class ChatService
    {
        private readonly IChatClient _chatClient;
        private readonly ILogger<ChatService> _logger;
        private readonly List<ChatMessage> _conversationHistory;
        public ChatService(
            IChatClient chatClient,
            ILogger<ChatService> logger) 
        {
            _chatClient = chatClient;
            _logger = logger;
            _conversationHistory = new List<ChatMessage>();
            InitializeSystemPrompt();
        }
        private void InitializeSystemPrompt()
        {
            var systemPrompt = @"
你是一个专业的智能客服助理，负责帮助客户解决各类问题。

你的职责包括：
1. 查询订单信息和物流状态
2. 处理退款和退货申请
3. 回答常见问题（FAQ）
4. 创建工单记录客户问题
5. 提供友好、专业的服务

工作原则：
- 始终保持礼貌和专业
- 主动询问必要信息（如订单号、客户ID等）
- 在执行操作前确认客户意图
- 如果无法解决问题，创建工单并告知客户会有人工客服跟进
- 使用简洁明了的语言回复

可用工具：
- QueryOrder: 查询订单详情
- QueryCustomerOrders: 查询客户所有订单
- RequestRefund: 申请退款
- CancelOrder: 取消订单
- SearchFAQ: 搜索常见问题
- GetFAQCategories: 获取FAQ分类
- CreateTicket: 创建工单
- QueryTicketStatus: 查询工单状态

请根据客户的问题，选择合适的工具来提供帮助。
";

            _conversationHistory.Add(new ChatMessage(ChatRole.System, systemPrompt.Trim()));
        }
        /// <summary>
        /// 发送消息并获取响应
        /// </summary>
        public async Task<string> SendMessageAsync(string userMessage, CancellationToken cancellationToken = default)
        {
            try
            {
                // 添加用户消息到历史
                _conversationHistory.Add(new ChatMessage(ChatRole.User, userMessage));

                // 准备工具集合
                var tools = new List<AITool>
            {
                AIFunctionFactory.Create(QueryOrder),
                AIFunctionFactory.Create(GetOrderTime),
            };

                // 配置 ChatOptions
                var options = new ChatOptions
                {
                    Temperature = 0.7f,
                    MaxOutputTokens = 1000,
                    Tools = tools,
                    ToolMode = ChatToolMode.Auto // 自动决定是否调用工具
                };            // 调用 AI（可能包含多轮工具调用）
                var response = await _chatClient.GetResponseAsync(_conversationHistory, options, cancellationToken);

                // 添加助手响应到历史
                _conversationHistory.Add(new ChatMessage(ChatRole.Assistant, response.Text ?? string.Empty));

                // 返回文本响应
                return response.Text ?? "(无响应)";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理消息时发生错误");
                return $"抱歉，处理您的请求时出现了问题：{ex.Message}";
            }
        }

        /// <summary>
        /// 根据订单号查询订单信息
        /// </summary>
        /// <param name="orderId">订单号</param>
        /// <returns>订单详细信息</returns>
        [Description("查询订单信息，根据订单号返回订单详情、状态、物流等信息")]
        public string QueryOrder(
            [Description("订单号，例如：12345")] string orderId)
        {
            var result = $"AI:{orderId}";
            return result;
        }
        /// 根据订单号查询订单信息
        /// </summary>
        /// <param name="orderId">订单号</param>
        /// <returns>订单详细信息</returns>
        [Description("返回订单时间")]
        public string GetOrderTime(
            [Description("订单号，例如：12345")] string orderId)
        {
            var result = DateTime.Now.ToString();
            return result;
        }
    }
}
