
using System.Linq;
using System;
using System.Collections.Generic;
using osu.Framework.Audio.Track;
using osu.Framework.Timing;
using System.Threading.Tasks;
using System.Diagnostics;
using osu.Framework.Logging;
using EmbedIO;
using EmbedIO.Actions;

namespace osu.Game.Beatmaps
{
    public sealed class SpotifyTrack : RemoteTrack
    {
        public override bool IsRunning => running;
        private bool running = false;
        public  bool pendingStateUpdate = false; // Todo: Make this get updated by changing the times

        public DateTimeOffset start = DateTimeOffset.Now; // time at which track started (updates after pauses)
        //public override double CurrentTime => DateTimeOffset.Now.Subtract(start).TotalMilliseconds;
        public override double CurrentTime
        {
            get
            {
                Logger.Log($"Getting CurrentTime: {DateTimeOffset.Now.Subtract(start).TotalMilliseconds}");
                return DateTimeOffset.Now.Subtract(start).TotalMilliseconds;
            }
        }

        public double currentTime {
            set {
                start = DateTimeOffset.Now.AddMilliseconds(-value);
            }
        }

        //public double pausedTime = 0; // Is this even necessary

        public SpotifyTrack(string reference, string name = "spotify")
            : base(reference, name)
        {
            Logger.Log($"Playing Spotify Track: {reference}");
            Length = 30000; // 30 seconds

            // Get authorization from RemoteAudioManager here
            /*SpotifyClientConfig? config = Authorize.CheckForAuthorization();
            if (config == null)
                config = Authorize.Pkce();

            log.Info($"Creating Player with token {Authorize.accessToken}");*/

            // perhaps it could pull this spotify object from the Singleton SpotifyAudioManager : RemoteAudioManager

            //spotify = new SpotifyClient(config);
            //log.Info($"Logged in as {spotify.UserProfile.Current().Result.DisplayName}");
            // Before allowing this to be created, make sure that OAuth is valid and RemoteAudioManager can carry out our wishes
        }

        public override bool Seek(double seek) // TODO: figure out why it calls Seek() so many times
        {
            if (pendingStateUpdate)
                return false;

            Logger.Log($"Seek to {seek}!!");

            pendingStateUpdate = true;
            currentTime = seek;
            SpotifyManager.Instance.Seek((long)seek);
            return seek > 0 && seek < Length;
        }

        //public override Task<bool> SeekAsync(double seek) => Task.FromResult(Seek(seek));

        public override void Start()
        {
            //if (pendingStateUpdate)
                //return;

            Logger.Log($"Start Track!!");

            currentTime = 0;
            pendingStateUpdate = true;
            //running = true;

            SpotifyManager.Instance.currentTrack = this;
            SpotifyManager.Instance.Play(reference);
        }

        /*public override Task StartAsync()
        {
            Start();
            return Task.CompletedTask;
        }*/

        public override void Reset()
        {
            //if (pendingStateUpdate)
                //return;

            Logger.Log("Reset Track!");

            pendingStateUpdate = true;
            Seek(0);
            base.Reset();
        }

        public override void Stop()
        {
            if (IsRunning && pendingStateUpdate)
            {
                Logger.Log($"Stop Track!!");

                //running = false;
                pendingStateUpdate = true;
                SpotifyManager.Instance.Stop();
            }
        }

        public void StateUpdate(long timestamp, long progress, bool paused)
        {
            Logger.Log($"State Updated: {timestamp} {start} {CurrentTime}");

            start = DateTimeOffset.FromUnixTimeMilliseconds(timestamp - progress);
            running = paused;
            pendingStateUpdate = false;
        }

        /*public override Task StopAsync()
        {
            Stop();
            return Task.CompletedTask;
        }*/
    }
}
