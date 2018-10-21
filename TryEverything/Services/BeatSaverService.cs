using SimpleJSON;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using TryEverything.Data;
using UnityEngine.Networking;

namespace TryEverything.Services
{
    class BeatSaverService : IBeatSaverService
    {
        private const string GetSongListUrl = "https://beatsaver.com/api/songs/new/";
        private const string GetSongByLevelUrl = "https://beatsaver.com/api/songs/search/hash/";
        private const int SongsPerPage = 20;
        private readonly string _beatSaberPath;

        public BeatSaverService(string beatSaberPath)
        {
            _beatSaberPath = beatSaberPath;
            ServicePointManager.ServerCertificateValidationCallback = (s, c, ch, e) => true;
        }

        public System.Collections.IEnumerator DownloadSong(CustomSong song)
        {
            var downloadFilename = Path.Combine(_beatSaberPath, "CustomSongs", song.Id, song.Id + ".zip");
            Directory.CreateDirectory(Path.GetDirectoryName(downloadFilename));

            var request = UnityWebRequest.Get(song.DownloadUri);
            var async = request.SendWebRequest();

            while (!async.isDone)
            {
                yield return null;
            }

            var data = request.downloadHandler.data;
            File.WriteAllBytes(downloadFilename, data);

            Console.WriteLine("[Plugins/TryEverything] Sond download complete: " + song.Title + ".");

            ZipFile.ExtractToDirectory(downloadFilename, Path.Combine(_beatSaberPath, "CustomSongs"));
            Directory.Delete(Path.GetDirectoryName(downloadFilename), true);
        }

        public IEnumerable<CustomSong> GetSongFromLevel(string levelId)
        {
            var request = UnityWebRequest.Get(GetSongByLevelUrl + levelId.Substring(0, 32));
            var async = request.SendWebRequest();

            while (!async.isDone)
            {
                yield return null;
            }

            var json = request.downloadHandler.text;

            yield return JsonToCustomSongs(json).FirstOrDefault();
        }

        public IEnumerable<IEnumerable<CustomSong>> GetSongs(int page = 1)
        {
            var request = UnityWebRequest.Get(GetSongListUrl + ((page - 1) * SongsPerPage));
            var async = request.SendWebRequest();

            while (!async.isDone)
            {
                yield return null;
            }

            var json = request.downloadHandler.text;

            yield return JsonToCustomSongs(json);
        }

        private static List<CustomSong> JsonToCustomSongs(string json)
        {
            var songs = JSON.Parse(json)["songs"];
            var result = new List<CustomSong>();

            for (var songIndex = 0; songIndex < songs.Count; songIndex++)
            {
                var difficultyLevels = 0;
                var difficulties = songs[songIndex]["difficulties"];

                for (var difficultyIndex = 0; difficultyIndex < difficulties.Count; difficultyIndex++)
                {
                    var difficultyName = difficulties[difficultyIndex]["difficulty"];
                    var difficultyLevel = (int)(DifficultyLevels)Enum.Parse(typeof(DifficultyLevels), difficultyName);

                    difficultyLevels = difficultyLevels | difficultyLevel;
                }

                result.Add(
                    new CustomSong(
                        songs[songIndex]["key"],
                        songs[songIndex]["songName"],
                        songs[songIndex]["songSubName"],
                        songs[songIndex]["uploader"],
                        songs[songIndex]["bpm"],
                        songs[songIndex]["upVotes"],
                        songs[songIndex]["downVotes"],
                        new Uri(songs[songIndex]["downloadUrl"]),
                        new Uri(songs[songIndex]["coverUrl"]),
                        (DifficultyLevels)difficultyLevels));
            }

            return result;
        }
    }
}
