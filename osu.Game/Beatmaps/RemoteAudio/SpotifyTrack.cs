
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
        //public override bool IsRunning => running;
        //private bool running = false;
        //private bool readyInWebSdk = false;
        public  bool pendingStateUpdate = false; // Todo: Make this get updated by changing the times

        //public DateTimeOffset start = DateTimeOffset.Now; // time at which track started (updates after pauses)
        //public override double CurrentTime => DateTimeOffset.Now.Subtract(start).TotalMilliseconds;

        /*public double currentTime {
            set {
                start = DateTimeOffset.Now.AddMilliseconds(-value);
            }
        }*/

        public SpotifyTrack(string reference, string name = "spotify")
            : base(30000, reference, name)
        {
            Logger.Log($"Creating Track: {reference}");
            //SpotifyManager.Instance.Play(reference); // TODO: Figure out where begin the web player
        }

        public override bool Seek(double seek)
        {
            Logger.Log($"Seeking to {seek} from SpotifyTrack at {CurrentTime}!!");

            if (Math.Abs(seek - CurrentTime) > 2000 && seek > 0 /*&& !pendingStateUpdate*/)
            {
                Logger.Log("need to seek here");
                //SpotifyManager.Instance.Seek((long)seek);
                pendingStateUpdate = true;
            }
            return base.Seek(seek);
        }

        public override void Start()
        {
            Logger.Log("START!!");
            Logger.Log($"Starting track {reference} from SpotifyTrack at {CurrentTime}!!");

            base.Start();
            pendingStateUpdate = true;

            SpotifyManager.Instance.currentTrack = this;
            SpotifyManager.Instance.Play(reference, (int)CurrentTime);
        }

        public override void Reset()
        {
            SpotifyManager.Instance.Reset();
            base.Reset();
        }

        public override void Stop()
        {
            Logger.Log("STOP!!!");
            Logger.Log($"Stopping Track {reference} from SpotifyTrack at {CurrentTime}");
            base.Stop();
            pendingStateUpdate = true;
            SpotifyManager.Instance.Stop();
        }

        public void StateUpdate(long timestamp, long progress, bool paused)
        {
            Logger.Log($"State Updated from {CurrentTime} to {progress}");
            pendingStateUpdate = false;
            //if (paused)
                //Stop();
//
            //if (Math.Abs(progress - CurrentTime) < 2000 && !pendingStateUpdate) // TODO: Calculate real progress from here
                //Seek(progress);

            //start = DateTimeOffset.FromUnixTimeMilliseconds(timestamp - progress);
            Logger.Log($"{CurrentTime} which should be {progress}");
        }
    }
}
