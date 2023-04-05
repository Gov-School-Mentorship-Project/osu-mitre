using System;
using osu.Framework.Logging;
using osu.Game.RemoteAudio;

namespace osu.Game.Beatmaps.RemoteAudio
{
    public sealed class SpotifyTrack : RemoteTrack
    {
        private bool startedOnRemote = false;

        public SpotifyTrack(string reference, double length, string name = "spotify")
            : base(length, reference, name)
        {
            Logger.Log($"Creating Track: {reference}");
            AggregateVolume.BindValueChanged(SpotifyManager.Instance.VolumeChanged);
            base.StartAsync();
            Completed += Stop;
        }

        public override bool Seek(double seek)
        {
            Logger.Log($"Seeking to {seek} from SpotifyTrack at {CurrentTime}!! length is {Length}");

            if (seek > 0)
            {
                Logger.Log("need to seek here");
                SpotifyManager.Instance.Seek((long)(seek));
            }
            return base.Seek(seek);
        }

        public override void Start()
        {
            Logger.Log("START!!");
            Logger.Log($"Starting track {reference} from SpotifyTrack at {CurrentTime}!!");
            base.Start();
            SpotifyManager.Instance.currentTrack = this;

            if (!startedOnRemote)
            {
                startedOnRemote = SpotifyManager.Instance.Play(reference, (int)(CurrentTime));
            }
            else
            {
                SpotifyManager.Instance.Resume((int)(CurrentTime));
            }
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
    }
}
