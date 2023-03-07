
using System;
using System.Diagnostics;
using System.Net.Http;
using Newtonsoft.Json;
using osu.Framework.Bindables;
using osu.Framework.Logging;
using System.Threading;

namespace osu.Game.Online.API.OAuthMethods
{
    public interface IOAuthPKCE
    {
        void AuthenticateWithPKCE(CancellationTokenSource cts);
    }
}
