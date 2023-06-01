# Playing osu!lazer With Remote Audio
This project intends to allow players of osu!lazer to play the game without the need of a local, digital copy of the music for their beatmaps. 
## Running 
With the repository cloned, this projet can be ran from the project rood directory with: 
```shell
dotnet run --project osu.Desktop
```
Complete details for working with and setting up osu!lazer can be found on the [official osu!lazer repository's](https://github.com/ppy/osu) README. 

## Connecting With Spotify
To connect with the Spotify API, you will need to create a [Spotify developer account](https://developer.spotify.com/) to get a client ID and client secret. This will allow this fork of osu!lazer to control a Spotify account by entering them in the new Remote Audio section of the settings menu. Once entered, click the "login" button to go through the Spotify OAuth process. Once done, click the "open webpage" to open up the container for the Spotify Web SDK, which has the control necessicary for this program to work. 

## Using This Program
To play songs using Spotify remote audio rather than the embedded audio, you must first add the Spotify URI to any beatmaps you want through the new input field in the editor. If creating beatmaps from scratch, you can use just the Spotify URI without the need for an audio file and it will load in the relavent information (artist, title, tempo, duration) direclty into the beatmap. The tempo changing mods do not work with the remote audio system.

## User Discretion
Be aware of the rules of using the Spotify API and music involved with this project. Feel free to fork this code and adapt it to make use of another music source or API that is more open to being used in games such as this. 
