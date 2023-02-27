using System.Linq;
using System;
using System.Collections.Generic;

namespace osu.Game.Beatmaps
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

        public static RemoteTrack? TrackFromReference(string reference)
        {
           if (SpotifyBeatmapAudio.validateRemoteAudio(reference, out _))
                return new SpotifyTrack(reference);

            return null;
        }
    }
}
