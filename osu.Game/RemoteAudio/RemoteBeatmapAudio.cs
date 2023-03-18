using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Beatmaps.RemoteAudio;
using System.Threading.Tasks;
using osu.Framework.Audio;

namespace osu.Game.RemoteAudio
{
    public static class RemoteBeatmapAudio
    {
        public static bool validateRemoteAudio(string reference, out string convertedReference)
        {
            return SpotifyBeatmapAudio.validateRemoteAudio(reference, out convertedReference);
        }

        public static bool validateRemoteAudio(string reference)
        {
            return SpotifyBeatmapAudio.validateRemoteAudio(reference, out _);
        }

        public static RemoteTrack? TrackFromReference(string reference, double length)
        {
            if (SpotifyBeatmapAudio.validateRemoteAudio(reference, out string convertedReference))
                return new SpotifyTrack(convertedReference, length);

            return null;
        }

        public static async Task<RemoteAudioInfo> GetRemoteBeatmapInfo(string reference)
        {
            if (validateRemoteAudio(reference, out string newRef))
            {
                return await SpotifyManager.Instance.GetRemoteBeatmapInfo(newRef).ConfigureAwait(false);
            }
            return new RemoteAudioInfo("Invalid reference", "Invalid Reference", 60, 1000);
        }

    }

    public struct RemoteAudioInfo
    {
        public RemoteAudioInfo(string artist, string title, double bpm, double length)
        {
            Artist = artist;
            Title = title;
            BPM = bpm;
            Length = length;
        }

        public string Artist { get; }
        public string Title { get; }
        public double BPM { get; }
        public double Length { get; }
    }
}
