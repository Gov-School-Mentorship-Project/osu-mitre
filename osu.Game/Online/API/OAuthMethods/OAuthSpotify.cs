
using System;
using System.Diagnostics;
using System.Net.Http;
using Newtonsoft.Json;
using osu.Framework.Bindables;
using osu.Framework.Logging;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Extensions;

namespace osu.Game.Online.API.OAuthMethods
{
    public class OAuthSpotify : OAuth, IOAuthPKCE
    {
        public OAuthSpotify(string clientId, string clientSecret, string endpoint) : base(clientId, clientSecret, endpoint) {}

        public async void AuthenticateWithPKCE()
        {
            Logger.Log("Generating Codes");
            (string verifier, string challenge) = PKCEUtil.GenerateCodes();
            Uri baseUri = new Uri("http://localhost:3000/callback");
            LoginRequest request = new LoginRequest(baseUri, clientId, LoginRequest.ResponseType.Code)
            {
                CodeChallengeMethod = "S256",
                CodeChallenge = challenge,
                Scope = new List<string> {
                    Scopes.UserReadEmail,
                    Scopes.PlaylistReadPrivate,
                    Scopes.UserModifyPlaybackState,
                    Scopes.UserReadPlaybackState,
                    Scopes.Streaming,
                    Scopes.UserReadPrivate,
                }
            };

            Logger.Log("Creating oauth Server");
            EmbedIOAuthServer server = new EmbedIOAuthServer(baseUri, 3000);
            Logger.Log("Open oauth Browser");

            // open spotify OAuth to get permission from user
            BrowserUtil.Open(request.ToUri());
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(120));

            Logger.Log("Starting oauth Server");
            server.Start().WaitSafely();

            string code = await WaitForCode(server, cts.Token).ConfigureAwait(true);
            Logger.Log("got code and now swapping for access token");

            // switch the PKCE code for an access token and refresh token
            PKCETokenRequest initialRequest = new PKCETokenRequest(clientId, code, baseUri, verifier);
            PKCETokenResponse initialResponse = await new OAuthClient().RequestToken(initialRequest).ConfigureAwait(false);

            Token.Value = new OAuthToken() {
                AccessToken = initialResponse.AccessToken,
                ExpiresIn = initialResponse.ExpiresIn,
                RefreshToken = initialResponse.RefreshToken
            };

            Logger.Log($"got access token: {initialResponse.AccessToken}");
            var authenticator = new PKCEAuthenticator(clientId, initialResponse);

            await server.Stop().ConfigureAwait(false);
        }

        static async Task<string> WaitForCode(EmbedIOAuthServer server, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

            Func<object, AuthorizationCodeResponse, Task> handler = (object sender, AuthorizationCodeResponse response) =>
            {
                Logger.Log("Authorization Code Received");
                tcs.TrySetResult(response.Code);
                return Task.CompletedTask;
            };

            Func<object, string, string?, Task> errorHandler = (object sender, string error, string? state) =>
            {
                Logger.Log(error);
                return Task.CompletedTask;
            };

            server.AuthorizationCodeReceived += handler; // register the event listener
            server.ErrorReceived += errorHandler;

            cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));

            string code = await tcs.Task.ConfigureAwait(false);
            server.AuthorizationCodeReceived -= handler;
            return code;
        }
    }
}
