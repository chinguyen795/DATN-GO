using System.Threading.Tasks;

namespace DATN_API.Services.Ai
{
    /// <summary>
    /// Interface cho dịch vụ AI chat cung cấp tính năng tư vấn thời trang
    /// </summary>
    public interface IAiChatService
    {
        /// <summary>
        /// Xử lý tin nhắn từ người dùng và trả về phản hồi được tạo bởi AI
        /// tập trung vào tư vấn thời trang và hỗ trợ thương mại điện tử
        /// </summary>
        /// <param name="message">Tin nhắn đầu vào từ người dùng</param>
        /// <returns>Phản hồi được tạo bởi AI dưới dạng chuỗi</returns>
        Task<string> AskAsync(string message);
    }
}