
using System;
using System.Diagnostics;
using System.Net.Http;
using Newtonsoft.Json;
using osu.Framework.Bindables;
using osu.Framework.Logging;

namespace osu.Game.Online.API.OAuthMethods
{
    public interface IOAuthPKCE
    {
        void AuthenticateWithPKCE();
    }
}
