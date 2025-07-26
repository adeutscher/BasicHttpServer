using RedShirt.BasicHttpServer.Responses;
using System.Threading.Tasks;

namespace RedShirt.BasicHttpServer.Structures
{
    public interface IHttpValidator
    {
        Task<ValidatorResponse> ValidateAsync(SimpleHttpRequest request);
    }
}