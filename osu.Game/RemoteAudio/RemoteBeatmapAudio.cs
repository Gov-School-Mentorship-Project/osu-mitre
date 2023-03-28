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
            return RemoteAudioInfo.DefaultError;
        }

    }

    public struct RemoteAudioInfo
    {
        public static RemoteAudioInfo DefaultError => new RemoteAudioInfo("Ensure you are logged in and the URI/URL is valid", "Ensure you are logged in and the URI/URL is valid", 1000, new List<Section>());

        public RemoteAudioInfo(string artist, string title, double length, List<Section> sections)
        {
            Artist = artist;
            Title = title;
            Length = length;
            Sections = sections;
        }

        public string Artist { get; }
        public string Title { get; }
        public double Length { get; }
        public List<Section> Sections { get; }
    }

    public struct Section
    {
        public Section(double beatDuration, double start, int timeNumerator, int timeDenominator)
        {
            BeatDuration = beatDuration;
            Start = start;
            TimeSignatureNumerator = timeNumerator;
            TimeSignatureDenominator = timeDenominator;
        }

        public double Start;
        public double BeatDuration;
        public int TimeSignatureNumerator { get; }
        public int TimeSignatureDenominator { get; }
    }
}
