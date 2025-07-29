using RedShirt.BasicHttpServer.Responses;
using RedShirt.BasicHttpServer.Structures;
using System;
using System.Text;
using System.Threading.Tasks;

namespace RedShirt.BasicHttpServer.Validators
{
    public class BasicHttpAuthorizationValidator : IHttpValidator
    {
        private readonly string _password;
        private readonly string _username;

        public BasicHttpAuthorizationValidator(string username, string password)
        {
            _username = username;
            _password = password;
        }

        public Task<ValidatorResponse> ValidateAsync(SimpleHttpRequest request)
        {
            if (!request.Headers.TryGetValue("Authorization", out var authorizationValue)
                || string.IsNullOrEmpty(authorizationValue))
            {
                return Task.FromResult(ValidatorResponse.AuthorizationRequired);
            }

            var topLevelParts = authorizationValue.Split(" ");
            if (topLevelParts.Length != 2 || topLevelParts[0] != "Basic")
            {
                return Task.FromResult(ValidatorResponse.AuthorizationRequired);
            }

            var parts = GetPasswordParts(topLevelParts[1]);
            if (parts.Length < 2)
            {
                return Task.FromResult(ValidatorResponse.BadRequest);
            }

            var username = parts[0];
            var password = parts[1];
            for (var i = 2; i < parts.Length; i++)
            {
                password += ':' + parts[i];
            }

            if (username == _username && password == _password)
            {
                return Task.FromResult(ValidatorResponse.Ok);
            }

            return Task.FromResult(ValidatorResponse.AuthorizationRequired);
            
        }

        internal static string[] GetPasswordParts(string authorizationValue)
        {
            var data = Convert.FromBase64String(authorizationValue);
            var decodedString = Encoding.UTF8.GetString(data);
            var parts = decodedString.Split(':');
            return parts;
        }
    }
}