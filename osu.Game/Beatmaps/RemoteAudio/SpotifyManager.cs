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
            accessToken = "BQBpyqiGZqTBf0yVQkdKXl4jrvuTTjYmh3K50XfzpZNUoO4idQVLm_equoL_S5Y5NzU9HzZS3Z-92TpeWI00RDyEyxxs7EJIgt8eGGsG1eVK-I6D_NUxKRUJ_nRsszb5LRWN6FzXaed7LOufjlHRdTkQ31nt5tTATDCPXi7nCFgDvHBP6mTU7F3b0lmtuvWQZ3XFa6jrIVfuTcXp00xdrd0Z";
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
