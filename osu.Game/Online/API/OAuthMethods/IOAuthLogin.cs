
using System;
using System.Diagnostics;
using System.Net.Http;
using Newtonsoft.Json;
using osu.Framework.Bindables;

namespace osu.Game.Online.API.OAuthMethods
{
    public interface IOAuthLogin
    {
        public void AuthenticateWithLogin(string username, string password);
    }
}
