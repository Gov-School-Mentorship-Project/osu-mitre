using System;
using System.Diagnostics;
using System.Net.Http;
using Newtonsoft.Json;
using osu.Framework.Bindables;

namespace osu.Game.Online.API.OAuthMethods
{
    public class OAuthOsu : OAuth, IOAuthLogin
    {

        public OAuthOsu(string clientId, string clientSecret, string endpoint) : base(clientId, clientSecret, endpoint) {}

        public void AuthenticateWithLogin(string username, string password)
        {
            if (string.IsNullOrEmpty(username)) throw new ArgumentException("Missing username.");
            if (string.IsNullOrEmpty(password)) throw new ArgumentException("Missing password.");

            var accessTokenRequest = new AccessTokenRequestPassword(username, password)
            {
                Url = $@"{endpoint}/oauth/token",
                Method = HttpMethod.Post,
                ClientId = clientId,
                ClientSecret = clientSecret
            };

            using (accessTokenRequest)
            {
                try
                {
                    accessTokenRequest.Perform();
                }
                catch (Exception ex)
                {
                    Token.Value = null;

                    var throwableException = ex;

                    try
                    {
                        // attempt to decode a displayable error string.
                        var error = JsonConvert.DeserializeObject<OAuthError>(accessTokenRequest.GetResponseString() ?? string.Empty);
                        if (error != null)
                            throwableException = new APIException(error.UserDisplayableError, ex);
                    }
                    catch
                    {
                    }

                    throw throwableException;
                }

                Token.Value = accessTokenRequest.ResponseObject;
            }
        }
    }
}
