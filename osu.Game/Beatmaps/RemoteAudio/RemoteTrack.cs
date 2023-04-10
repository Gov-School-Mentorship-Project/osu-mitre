
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
using osu.Game.RemoteAudio;

namespace osu.Game.Beatmaps.RemoteAudio
{
    public class RemoteTrack : Track
    {
        private readonly StopwatchClock clock = new StopwatchClock();

        public override bool IsDummyDevice => false;

        protected double seekOffset;

        public string reference;

        public RemoteTrack(double length, string reference, string name = "virtual")
            : base(name)
        {
            Length = length;
            this.reference = reference;
        }

        public override bool Seek(double seek)
        {
            seekOffset = Math.Clamp(seek, 0, Length);

            lock (clock)
            {
                if (IsRunning)
                    clock.Restart();
                else
                    clock.Reset();
            }

            return seekOffset == seek;
        }

        public override Task<bool> SeekAsync(double seek)
        {
            return Task.FromResult(Seek(seek));
        }

        public override Task StartAsync()
        {
            Start();
            return Task.CompletedTask;
        }

        public override Task StopAsync()
        {
            Stop();
            return Task.CompletedTask;
        }

        public override void Start()
        {
            Logger.Log("Start remote track");
            if (Length == 0)
                return;

            lock (clock) clock.Start();
        }

        public override void Reset()
        {
            Logger.Log("Reset Remote Track");
            lock (clock) clock.Reset();
            seekOffset = 0;

            base.Reset();
        }

        public override void Stop()
        {
            Logger.Log("stop remote track");
            lock (clock) clock.Stop();
        }

        public override bool IsRunning
        {
            get
            {
                lock (clock) return clock.IsRunning;
            }
        }

        public override double CurrentTime
        {
            get
            {
                if (clock.IsRunning)
                {
                    UpdateState();
                }
                lock (clock) return Math.Min(Length, seekOffset + clock.CurrentTime);
            }
        }

        protected override void UpdateState()
        {
            //base.UpdateState();

            lock (clock)
            {
                if (clock.IsRunning && clock.CurrentTime + seekOffset >= Length)
                {
                    clock.Stop();
                    if (Looping)
                    {
                        Logger.Log($"should reset track?? at {RestartPoint}");
                        Stop();
                        Seek(0);
                        Start();
                    }
                    else
                    {
                        Logger.Log("completed track, should stop now :)");
                        //Stop(); // Make sure to avoid infinite loops
                        RaiseCompleted();
                    }
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
                Stop();

            base.Dispose(disposing);
        }
    }
}
