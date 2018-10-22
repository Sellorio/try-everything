using SongLoaderPlugin;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TryEverything.Data;
using TryEverything.Helpers;
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

        public bool IsDownloadingSongs { get; private set; }
        public bool HasPendingSongRefresh { get; private set; }

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
                    ? File.ReadAllLines(PendingSongsFilename).Where(x => Directory.Exists(Path.Combine(BeatSaberPath, "CustomSongs", FilesystemHelper.SanitiseForPath(x)))).ToList()
                    : new List<string>();
        }

        public void Start()
        {
            if (_pendingSongs.Count < 5)
            {
                StartCoroutine(GetSongBatch());
            }
        }

        public void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        public void Update()
        {
            if (HasPendingSongRefresh && SongLoader.Instance != null)
            {
                Console.WriteLine("[Plugins/TryEverything] Attempting to refresh songs list...");
                HasPendingSongRefresh = false;
                SongLoader.Instance.RefreshSongs(true);
                Console.WriteLine("[Plugins/TryEverything] Songs list refreshed.");
            }
        }

        public bool IsPendingSong(CustomSong song)
        {
            if (song != null)
            {
                return _pendingSongs.Contains(song.Title);
            }

            return false;
        }

        public bool AcceptSong(CustomSong song)
        {
            var result = false;

            if (_pendingSongs.Contains(song.Title))
            {
                _pendingSongs.Remove(song.Title);
                File.WriteAllLines(PendingSongsFilename, _pendingSongs);
                result = true;
            }

            return result;
        }

        public bool RejectSong(string title)
        {
            var result = false;

            if (_pendingSongs.Contains(title))
            {
                if (!_rejectedSongs.Contains(title))
                {
                    _rejectedSongs.Add(title);
                    File.WriteAllLines(RejectedSongsFilename, _rejectedSongs);
                }

                var songPath = Path.Combine(BeatSaberPath, "CustomSongs", FilesystemHelper.SanitiseForPath(title));

                if (Directory.Exists(songPath))
                {
                    Directory.Delete(songPath, true);
                }

                _pendingSongs.Remove(title);
                File.WriteAllLines(PendingSongsFilename, _pendingSongs);

                result = true;
            }

            return result;
        }

        public bool RejectSong(CustomSong song)
        {
            return RejectSong(song?.Title);
        }

        public bool IgnoreAuthor(CustomSong song)
        {
            var result = false;

            if (!_ignoredAuthors.Contains(song.AuthorName))
            {
                _ignoredAuthors.Add(song.AuthorName);
                File.WriteAllLines(IgnoredAuthorsFilename, _ignoredAuthors);

                var songPath = Path.Combine(BeatSaberPath, "CustomSongs", FilesystemHelper.SanitiseForPath(song.Title));

                if (Directory.Exists(songPath))
                {
                    Directory.Delete(songPath, true);
                }

                _pendingSongs.Remove(song.Title);
                File.WriteAllLines(PendingSongsFilename, _pendingSongs);

                result = true;
            }

            return result;
        }

        /// <summary>
        /// Loads new songs until the number of pending songs is <see cref="MaximumPendingSongs"/> and then
        /// refreshes the song loader.
        /// </summary>
        public System.Collections.IEnumerator GetSongBatch()
        {
            var songPage = 1;
            var songsToDownload = new List<CustomSong>();

            while (_pendingSongs.Count < 5)
            {
                IsDownloadingSongs = true;

                IEnumerable<CustomSong> songs = null;

                foreach (var yield in _beatSaverService.GetSongs(songPage))
                {
                    songs = yield;
                    yield return null;
                }
                
                foreach (var song in songs)
                {
                    if (!_ignoredAuthors.Contains(song.AuthorName)
                        && !_rejectedSongs.Contains(song.Title)
                        && !_pendingSongs.Contains(song.Title)
                        && !Directory.Exists(Path.Combine(BeatSaberPath, "CustomSongs", FilesystemHelper.SanitiseForPath(song.Title)))) // not manually downloaded before
                    {
                        _pendingSongs.Add(song.Title);
                        songsToDownload.Add(song);

                        if (_pendingSongs.Count == MaximumPendingSongs)
                        {
                            break;
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
                
                File.WriteAllLines(PendingSongsFilename, _pendingSongs);

                HasPendingSongRefresh = true;
            }

            IsDownloadingSongs = false;
        }
    }
}
