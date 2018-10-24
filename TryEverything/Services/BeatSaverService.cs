using SimpleJSON;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TryEverything.Data;
using TryEverything.Helpers;
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

            Console.WriteLine("[Plugins/TryEverything] Sond download complete: " + song.Title + " mapped by " + song.AuthorName + ".");

            // if the song author didn't put a song folder as the root of the zip file then we need to do that ourselves.
            bool needSubFolder = false;
            // if the zip is corrupted then try downloading again.
            bool tryAgain = false;

            try
            {
                using (var archive = ZipFile.OpenRead(downloadFilename))
                {
                    needSubFolder = archive.Entries.Any(x => !x.FullName.Contains(Path.DirectorySeparatorChar));
                }
            }
            catch (InvalidDataException ex)
            {
                if (ex.Message == "End of Central Directory record could not be found.") // corrupt zip file, try downloading again
                {
                    tryAgain = true;
                }
                else
                {
                    Plugin.Log(ex.ToString());
                    yield break;
                }
            }

            if (tryAgain)
            {
                File.Delete(downloadFilename);

                request = UnityWebRequest.Get(song.DownloadUri);
                async = request.SendWebRequest();

                while (!async.isDone)
                {
                    yield return null;
                }

                data = request.downloadHandler.data;
                File.WriteAllBytes(downloadFilename, data);

                try
                {
                    using (var archive = ZipFile.OpenRead(downloadFilename))
                    {
                        needSubFolder = archive.Entries.Any(x => !x.FullName.Contains(Path.DirectorySeparatorChar));
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Log(ex.ToString());
                    yield break;
                }
            }

            var extractToPath = Path.Combine(_beatSaberPath, "CustomSongs");

            if (needSubFolder)
            {
                extractToPath = Path.Combine(extractToPath, FilesystemHelper.SanitiseForPath(song.Title));
                Directory.CreateDirectory(extractToPath);
            }

            Plugin.Log("Starting new thread to unzip song data...");

            var ioCompletedSemaphore = new SemaphoreSlim(0);
            
            new Thread(
                () =>
                {
                    try
                    {
                        ZipFile.ExtractToDirectory(downloadFilename, extractToPath);
                        Directory.Delete(Path.GetDirectoryName(downloadFilename), true);
                    }
                    catch (Exception ex)
                    {
                        Plugin.Log("Failed to extract song data: " + ex.ToString());
                    }

                    ioCompletedSemaphore.Release();
                }).Start();

            while (ioCompletedSemaphore.CurrentCount == 0)
            {
                yield return null;
            }

            Plugin.Log("Song extracted and ready to go!");
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
                        songs[songIndex]["id"],
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
