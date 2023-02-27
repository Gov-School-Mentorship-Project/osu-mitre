
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
using EmbedIO.WebSockets;

namespace osu.Game.Beatmaps
{
    public class RemoteTrack : Track
    {
        protected readonly StopwatchClock referenceClock;
        protected string reference;
        public override bool IsRunning => referenceClock.IsRunning;

        public override double CurrentTime => referenceClock.CurrentTime;

        public RemoteTrack(string reference, string name = "remote")
            : base(name)
        {
            RemoteBeatmapAudio.validateRemoteAudio(reference, out this.reference);
            referenceClock = new StopwatchClock();
            Length = 30000; // 30 seconds
        }

        public override bool Seek(double seek)
        {
            referenceClock.Seek(seek);
            Logger.Log($"Seek to {seek}!!");
            return seek > 0 && seek < Length;
        }

        public override Task<bool> SeekAsync(double seek) => Task.FromResult(Seek(seek));

        public override void Start()
        {
            referenceClock.Start();
            Logger.Log($"Start Track!!");
        }

        public override Task StartAsync()
        {
            Start();
            return Task.CompletedTask;
        }

        public override void Reset()
        {
            Logger.Log("Reset Track!");
            Seek(0);
            base.Reset();
        }

        public override void Stop()
        {
            if (IsRunning)
            {
                referenceClock.Stop();
                Logger.Log($"Stop Track!!");
            }
        }

        public override Task StopAsync()
        {
            Stop();
            return Task.CompletedTask;
        }

    }
}
