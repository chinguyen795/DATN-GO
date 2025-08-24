using System.Threading.Tasks;

namespace DATN_API.Services.Ai
{
    public interface IAiChatService
    {
        Task<string> AskAsync(string message);
    }
}