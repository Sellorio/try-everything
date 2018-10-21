using SongLoaderPlugin;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TryEverything.Data;
using TryEverything.Services;
using UnityEngine;

namespace TryEverything.UI
{
    class TryEverythingHost : MonoBehaviour
    {
        private const int MaximumPendingSongs = 5;
        private static readonly string IgnoredAuthorsFilename;
        private static readonly string RejectedSongsFilename;
        private static readonly string PendingSongsFilename;

        public static string BeatSaberPath { get; }

        private readonly IBeatSaverService _beatSaverService;
        private readonly ISongLoadingService _songLoadingService; // not in use at the moment but will be if a more significant UI is added for this plugin

        private readonly List<string> _ignoredAuthors;
        private readonly List<string> _rejectedSongs;
        private readonly List<string> _pendingSongs;
        private readonly object _lock = new object();

        //private bool _hasPendingSongRefresh;

        static TryEverythingHost()
        {
            BeatSaberPath = Application.dataPath;
            BeatSaberPath = BeatSaberPath.Substring(0, BeatSaberPath.Length - 5);
            BeatSaberPath = BeatSaberPath.Substring(0, BeatSaberPath.LastIndexOf("/"));
            IgnoredAuthorsFilename = Path.Combine(BeatSaberPath, "UserData", "Everything.IgnoredAuthors.txt");
            RejectedSongsFilename = Path.Combine(BeatSaberPath, "UserData", "Everything.RejectedSongs.txt");
            PendingSongsFilename = Path.Combine(BeatSaberPath, "UserData", "Everything.PendingSongs.txt");
        }

        public TryEverythingHost()
            : this(new BeatSaverService(BeatSaberPath), new SongLoadingService())
        {
        }

        public TryEverythingHost(IBeatSaverService beatSaverService, ISongLoadingService songLoadingService)
        {
            _beatSaverService = beatSaverService;
            _songLoadingService = songLoadingService;

            _ignoredAuthors = File.Exists(IgnoredAuthorsFilename) ? new List<string>(File.ReadAllLines(IgnoredAuthorsFilename)) : new List<string>();
            _rejectedSongs = File.Exists(RejectedSongsFilename) ? new List<string>(File.ReadAllLines(RejectedSongsFilename)) : new List<string>();

            // only including songs that are still in the CustomSongs folder (were not removed outside of this plugin)
            _pendingSongs =
                File.Exists(PendingSongsFilename)
                    ? File.ReadAllLines(PendingSongsFilename).Where(x => Directory.Exists(Path.Combine(BeatSaberPath, "CustomSongs", x))).ToList()
                    : new List<string>();
        }

        public void Start()
        {
            if (_pendingSongs.Count < 5)
            {
                StartCoroutine(GetSongBatch());
            }
        }

        public void Update()
        {
            //if (_hasPendingSongRefresh)
            //{
            //    SongLoader.Instance.RefreshSongs(false);
            //}
        }

        public bool IsPendingSong(CustomSong song)
        {
            if (song != null)
            {
                lock (_lock)
                {
                    return _pendingSongs.Contains(song.Id);
                }
            }

            return false;
        }

        public void AcceptSong(CustomSong song)
        {
            lock (_lock)
            {
                if (_pendingSongs.Contains(song.Id))
                {
                    _pendingSongs.Remove(song.Id);
                    File.WriteAllLines(PendingSongsFilename, _pendingSongs);
                }
            }
        }

        public void RejectSong(CustomSong song)
        {
            lock (_lock)
            {
                if (_pendingSongs.Contains(song.Id))
                {
                    if (!_rejectedSongs.Contains(song.Id))
                    {
                        _rejectedSongs.Add(song.Id);
                        File.WriteAllLines(RejectedSongsFilename, _rejectedSongs);
                    }

                    if (Directory.Exists(Path.Combine(BeatSaberPath, "CustomSongs", song.Title)))
                    {
                        Directory.Delete(Path.Combine(BeatSaberPath, "CustomSongs", song.Title));
                    }

                    _pendingSongs.Remove(song.Id);
                    File.WriteAllLines(PendingSongsFilename, _pendingSongs);
                }
            }
        }

        /// <summary>
        /// Loads new songs until the number of pending songs is <see cref="MaximumPendingSongs"/> and then
        /// refreshes the song loader.
        /// </summary>
        public System.Collections.IEnumerator GetSongBatch()
        {
            Console.WriteLine("[Plugins/TryEverything] Checking what songs need to be downloaded...");

            var songPage = 1;
            var songsToDownload = new List<CustomSong>();

            while (_pendingSongs.Count < 5)
            {
                IEnumerable<CustomSong> songs = null;

                foreach (var yield in _beatSaverService.GetSongs(songPage))
                {
                    songs = yield;
                    yield return null;
                }

                lock (_lock)
                {
                    foreach (var song in songs)
                    {
                        if (!_ignoredAuthors.Contains(song.AuthorName)
                            && !_rejectedSongs.Contains(song.Id)
                            && !_pendingSongs.Contains(song.Id)
                            && !Directory.Exists(Path.Combine(BeatSaberPath, "CustomSongs", song.Title))) // not manually downloaded before
                        {
                            _pendingSongs.Add(song.Id);
                            songsToDownload.Add(song);

                            if (_pendingSongs.Count == MaximumPendingSongs)
                            {
                                break;
                            }
                        }
                    }
                }

                songPage++;
            }

            Console.WriteLine("[Plugins/TryEverything] Downloading " + songsToDownload.Count + " songs...");

            if (songsToDownload.Count > 0)
            {
                foreach (var song in songsToDownload)
                {
                    yield return _beatSaverService.DownloadSong(song);
                }

                lock (_lock)
                {
                    File.WriteAllLines(PendingSongsFilename, _pendingSongs);
                }

                Console.WriteLine("[Plugins/TryEverything] Refreshing songs after GetSongBatchInternal completed...");
                SongLoader.Instance.RefreshSongs(false);
                Console.WriteLine("[Plugins/TryEverything] Songs refreshed.");
            }
        }
    }
}
