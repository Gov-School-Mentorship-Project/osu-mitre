using System.Text.RegularExpressions;

namespace osu.Game.Beatmaps
{
    static class SpotifyBeatmapAudio // I wish there were a way to make this inherit from a super class to handle all RemoteAudio stuff
    {
        public static bool validateRemoteAudio(string reference)
        {
            Regex uriMatch = new Regex(@"\G\s*spotify:track:[A-Za-z0-9]{22}\s*");
            if (uriMatch.IsMatch(reference))
                return true ;

            Regex urlMatch = new Regex(@"\G\s*(http[s]?:\/\/)?open\.spotify\.com\/track/(?<id>[A-Za-z0-9]{22})([?](.+)?)?$");
            Match m = urlMatch.Match(reference);
            return m.Success;

            // TODO: use this to change form url to uri form

            /*if (m.Success && m.Groups.TryGetValue("id", out Group? id))
            {
                uri = $"spotify:track:{id.Value}";
                return true;
            }
            uri = "";
            return false;
            return false;*/

        }
    }
}
