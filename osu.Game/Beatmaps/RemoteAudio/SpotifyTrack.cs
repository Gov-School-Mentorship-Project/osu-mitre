
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

namespace osu.Game.Beatmaps.RemoteAudio
{
    public sealed class SpotifyTrack : RemoteTrack
    {
        public SpotifyTrack(string reference, double length, string name = "spotify")
            : base(length, reference, name)
        {
            Logger.Log($"Creating Track: {reference}");
            SpotifyManager.Instance.Play(reference, (int)CurrentTime);
            SpotifyManager.Instance.Volume(AggregateVolume.Value);
            SpotifyManager.Instance.audio?.AggregateVolume.BindValueChanged(VolumeChanged);
            //SpotifyManager.Instance.Play(reference); // TODO: Figure out where begin the web player
        }

        public override bool Seek(double seek)
        {
            Logger.Log($"Seeking to {seek} from SpotifyTrack at {CurrentTime}!!");

            if (Math.Abs(seek - CurrentTime) > 2000 && seek > 0)
            {
                Logger.Log("need to seek here");
                SpotifyManager.Instance.Seek((long)seek);
            }
            return base.Seek(seek);
        }

        public override void Start()
        {
            Logger.Log("START!!");
            Logger.Log($"Starting track {reference} from SpotifyTrack at {CurrentTime}!!");

            base.Start();

            SpotifyManager.Instance.currentTrack = this;
            SpotifyManager.Instance.Resume((int)CurrentTime);
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

        private void VolumeChanged(ValueChangedEvent<double> volume)
        {
            Logger.Log($"Volume Changed {volume.NewValue}");
            SpotifyManager.Instance.Volume(volume.NewValue); // TODO: Figure out why this isn't being called!!!
        }

        public void StateUpdate(long timestamp, long progress, bool paused)
        {
            Logger.Log($"State Updated from {CurrentTime} to {progress}");
            Logger.Log($"{CurrentTime} which should be {progress}");
        }
    }
}
