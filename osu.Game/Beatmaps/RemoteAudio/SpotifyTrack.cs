
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
using osu.Game.RemoteAudio;

namespace osu.Game.Beatmaps.RemoteAudio
{
    public sealed class SpotifyTrack : RemoteTrack
    {
        public override bool IsRunning => running;
        private bool running = false;
        //private bool readyInWebSdk = false;
        public  bool pendingStateUpdate = false; // Todo: Make this get updated by changing the times

        public DateTimeOffset start = DateTimeOffset.Now; // time at which track started (updates after pauses)
        public override double CurrentTime => DateTimeOffset.Now.Subtract(start).TotalMilliseconds;

        public double currentTime {
            set {
                start = DateTimeOffset.Now.AddMilliseconds(-value);
            }
        }

        public SpotifyTrack(string reference, string name = "spotify")
            : base(reference, name)
        {
            Logger.Log($"Creating Track: {reference}");
            Length = 30000; // 30 seconds

            SpotifyManager.Instance.Play(reference); // TODO: Figure out where begin the web player
            //readyInWebSdk = true;
        }

        public override bool Seek(double seek)
        {
            if (pendingStateUpdate || seek < 0)
                return false;

            Logger.Log($"Seeking to {seek} from SpotifyTrack at {CurrentTime}!!");

            pendingStateUpdate = true;
            currentTime = seek;
            SpotifyManager.Instance.Seek((long)seek);
            return seek > 0 && seek < Length;
        }

        //public override Task<bool> SeekAsync(double seek) => Task.FromResult(Seek(seek));

        public override void Start()
        {
            Logger.Log($"Starting track {reference} from SpotifyTrack at {CurrentTime}!!");

            currentTime = 0;
            pendingStateUpdate = true;

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
            Logger.Log("Reseting Track {reference} from SpotifyTrack at {CurrentTime}");

            pendingStateUpdate = true;
            Seek(0);
            base.Reset();
        }

        public override void Stop()
        {
            if (IsRunning && pendingStateUpdate)
            {
                Logger.Log("Stopping Track {reference} from SpotifyTrack at {CurrentTime}");

                //running = false;
                pendingStateUpdate = true;
                SpotifyManager.Instance.Stop();
            }
        }

        public void StateUpdate(long timestamp, long progress, bool paused)
        {
            Logger.Log($"State Updated from {CurrentTime} to");
            start = DateTimeOffset.FromUnixTimeMilliseconds(timestamp - progress);
            Logger.Log($"{CurrentTime} which should be {progress}");
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
