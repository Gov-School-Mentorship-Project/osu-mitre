using System.Text.RegularExpressions;

namespace osu.Game.RemoteAudio
{
    internal static class SpotifyBeatmapAudio // I wish there were a way to make this inherit from a super class to handle all RemoteAudio stuff
    {
        public static bool validateRemoteAudio(string reference, out string convertedReference)
        {
            Regex uriMatch = new Regex(@"\G\s*spotify:track:[A-Za-z0-9]{22}\s*");
            if (uriMatch.IsMatch(reference))
            {
                convertedReference = reference;
                return true;
            }

            Regex urlMatch = new Regex(@"\G\s*(http[s]?:\/\/)?open\.spotify\.com\/track/(?<id>[A-Za-z0-9]{22})([?](.+)?)?$");
            Match m = urlMatch.Match(reference);
            if (m.Success && m.Groups.TryGetValue("id", out Group? id))
            {
                convertedReference = $"spotify:track:{id.Value}";
                return true;
            }

            convertedReference = "";
            return false;
        }
    }
}
