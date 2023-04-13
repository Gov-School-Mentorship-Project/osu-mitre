
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

        public async void AuthenticateWithPKCE(CancellationTokenSource cts)
        {
            Logger.Log("Generating Codes");
            (string verifier, string challenge) = PKCEUtil.GenerateCodes();
            Uri baseUri = new Uri("http://localhost:3000/callback");
            if (String.IsNullOrEmpty(clientId))
            {
                Logger.Log("Missing ClientId", level: LogLevel.Error);
            }
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

            Logger.Log("Starting oauth Server");
            server.Start().WaitSafely();

            string code;
            try {
                code = await WaitForCode(server, cts.Token).ConfigureAwait(false);
                Logger.Log("Received code and now swapping for access token");
            } catch (OperationCanceledException)
            {
                Logger.Log("Spotify Login Canceled!");
                await server.Stop().ConfigureAwait(false);
                return;
            }

            // switch the PKCE code for an access token and refresh token
            PKCETokenRequest initialRequest = new PKCETokenRequest(clientId, code, baseUri, verifier);
            PKCETokenResponse initialResponse;
            try
            {
                initialResponse = await new OAuthClient().RequestToken(initialRequest).ConfigureAwait(false);
            } catch (SpotifyAPI.Web.APIException)
            {
                Logger.Log("Error switching grant for access token");
                cts.Cancel();
                await server.Stop().ConfigureAwait(false);
                return;
            } catch (HttpRequestException)
            {
                Logger.Log("Error getting Spotify access token", level: LogLevel.Error);
                return;
            }

            Logger.Log($"got access token");

            Token.Value = new OAuthToken() {
                AccessToken = initialResponse.AccessToken,
                ExpiresIn = initialResponse.ExpiresIn,
                RefreshToken = initialResponse.RefreshToken
            };

            //var authenticator = new PKCEAuthenticator(clientId, initialResponse);

            await server.Stop().ConfigureAwait(false);
        }

        static Task<string> WaitForCode(EmbedIOAuthServer server, CancellationToken cancellationToken)
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

            cancellationToken.Register(() => {
                tcs.TrySetCanceled(cancellationToken);
            });

            return tcs.Task;
        }
    }
}
