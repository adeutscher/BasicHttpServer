using RedShirt.BasicHttpServer.Responses;
using RedShirt.BasicHttpServer.Structures;
using System.Threading.Tasks;

namespace RedShirt.BasicHttpServer.Demo.Validators
{
    /// <summary>
    ///     Demo of a validator
    /// </summary>
    public class BasicHttpAddressValidator : IHttpValidator
    {
        private readonly string _acceptedAddress;
        private readonly DemoUIHandler _uiHandler;

        public BasicHttpAddressValidator(string acceptedAddress, DemoUIHandler uiHandler)
        {
            _acceptedAddress = acceptedAddress;
            _uiHandler = uiHandler;
        }

        public Task<ValidatorResponse> ValidateAsync(SimpleHttpRequest request)
        {
            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (request.SourceAddress == _acceptedAddress)
            {
                return Task.FromResult(ValidatorResponse.Ok);
            }

            _uiHandler.AddToast($"Rejected HTTP request from '{request.SourceAddress}'");
            return Task.FromResult(ValidatorResponse.Forbidden);
        }
    }
}