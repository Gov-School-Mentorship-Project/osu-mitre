
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
using osu.Framework.Bindables;
using osu.Framework.Allocation;

namespace osu.Game.Beatmaps.RemoteAudio
{
    public sealed class SpotifyTrack : RemoteTrack
    {
        public SpotifyTrack(string reference, double length, string name = "spotify")
            : base(length, reference, name)
        {
            Logger.Log($"Creating Track: {reference}");
            SpotifyManager.Instance.Play(reference, (int)(CurrentTime + seekOffset));
            AggregateVolume.BindValueChanged(SpotifyManager.Instance.VolumeChanged);
            //SpotifyManager.Instance.Volume(SpotifyManager.Instance.audio?.AggregateVolume.Value ?? 1);
            //SpotifyManager.Instance.Play(reference); // TODO: Figure out where begin the web player
        }

        public override bool Seek(double seek)
        {
            Logger.Log($"Seeking to {seek} from SpotifyTrack at {CurrentTime}!!");

            if (Math.Abs(seek - (CurrentTime + seekOffset)) > 2000 && seek + seekOffset > 0)
            {
                Logger.Log("need to seek here");
                SpotifyManager.Instance.Seek((long)(seek + seekOffset));
            }
            return base.Seek(seek);
        }

        public override void Start()
        {
            Logger.Log("START!!");
            Logger.Log($"Starting track {reference} from SpotifyTrack at {CurrentTime}!!");

            base.Start();

            SpotifyManager.Instance.currentTrack = this;
            SpotifyManager.Instance.Resume((int)(CurrentTime + seekOffset));
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
            SpotifyManager.Instance.Stop(this);
        }

        public void StateUpdate(long timestamp, long progress, bool paused) // TODO: Remove this
        {
            Logger.Log($"State Updated from {CurrentTime} to {progress}");
            Logger.Log($"{CurrentTime} which should be {progress}");
        }
    }
}
