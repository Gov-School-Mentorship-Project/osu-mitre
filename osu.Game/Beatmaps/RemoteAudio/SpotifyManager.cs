using SpotifyAPI.Web;
using System.Collections.Generic;
using osu.Framework.Logging;
using EmbedIO;
using System;
using EmbedIO.Actions;

namespace osu.Game.Beatmaps
{
    public sealed class SpotifyManager // This should be a singleton to store all authorization and connections with the Spotify API
    {
        private SpotifyClient? spotify;
        private WebServer server;
        public SpotifyTrack? currentTrack;
        public bool ready = false; // Has the web playback connected and has the device been transfered?
        public string? accessToken
        {
            get => _accessToken;
            set {
                if (value != null)
                {
                    _accessToken = value;
                    spotify = new SpotifyClient(value);
                }
            }
        }

        private string? _accessToken;
        public string? deviceId;

        private static readonly SpotifyManager instance;
        public static SpotifyManager Instance => instance;

        static SpotifyManager()
        {
            instance = new SpotifyManager();
        }

        private SpotifyManager() // Figure out when is the best time to create the spotify object... after OAuth?
        {
            Logger.Log("Creating Spotify Server!!!!");
            server = RemoteAudioServerFactory.CreateSpotifyServer("http://localhost:9999");
            server.RunAsync();
            accessToken = "BQB-bovdawUibTgwyNkcAz4AdQ3vnAZLu8RNaiJEe1IgnX5ToBsp4o0KyD3uL6UeG7BN5GSyNAcO3e3RE-GvRPGX89RwaGbII8p-9rxdXH0vGkJdGYLJ_0Cq92leKVxhnOEhwhfxir1iMvGwbJ_HXR_7urFaclA2q2533HafNMNfN0I0QDmmNqOvLc1LodPkX0rv6zuRBVcjhzGQSntPVowK";
        }

        public static void Init()
        {
        }

        public void TransferDevice(string deviceId)
        {
            Logger.Log($"Trying to transfer device to {deviceId}");
            if (spotify == null)
                return;

            spotify.Player.TransferPlayback(new PlayerTransferPlaybackRequest(new List<string> { deviceId }));
            ready = true;
        }

        public void Play(string reference) // Opens the provided input in Spotify. Checks if URL or URI is in valid format, but not if it links to an actual track
        {
            if (!ready || spotify == null) {
                Logger.Log("Cannot Play Until Web Deivce Is Registered");
                return;
            }

            // Maybe validate uri here

            PlayerResumePlaybackRequest resume = new PlayerResumePlaybackRequest() { Uris = new List<string> { reference } };
            spotify.Player.ResumePlayback(resume);
        }

        public void Resume()
        {
            if (!ready || spotify == null) {
                Logger.Log("Cannot Play Until Web Deivce Is Registered");
                return;
            }

            spotify.Player.ResumePlayback(new PlayerResumePlaybackRequest());
        }

        public void Stop()
        {
            if (!ready || spotify == null) {
                Logger.Log("Cannot Play Until Web Deivce Is Registered");
                return;
            }

            spotify.Player.PausePlayback(new PlayerPausePlaybackRequest());
        }

        public void Seek(long ms)
        {
            if (!ready || spotify == null) {
                Logger.Log("Cannot Play Until Web Deivce Is Registered");
                return;
            }

            spotify.Player.SeekTo(new PlayerSeekToRequest(Math.Max(0, ms)));
        }

    }
}
