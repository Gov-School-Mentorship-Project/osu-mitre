using System.Linq;
using System;
using System.Collections.Generic;

namespace osu.Game.Beatmaps
{
    public static class RemoteBeatmapAudio
    {
        public static bool validateRemoteAudio(string reference)
        {
            return SpotifyBeatmapAudio.validateRemoteAudio(reference);
        }
    }
}
