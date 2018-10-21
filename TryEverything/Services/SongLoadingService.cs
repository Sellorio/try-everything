using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TryEverything.Data;
using UnityEngine;
using UnityEngine.Networking;

namespace TryEverything.Services
{
    class SongLoadingService : ISongLoadingService
    {
        private static readonly Dictionary<string, Sprite> _songArtSpritCache = new Dictionary<string, Sprite>();
        private static readonly ReaderWriterLockSlim _songArtSpritCacheLock = new ReaderWriterLockSlim();

        public async Task<Sprite> LoadArt(CustomSong song)
        {
            _songArtSpritCacheLock.EnterReadLock();

            if (_songArtSpritCache.ContainsKey(song.Id))
            {
                var result = _songArtSpritCache[song.Id];
                _songArtSpritCacheLock.ExitReadLock();
                return result;
            }
            else
            {
                _songArtSpritCacheLock.ExitReadLock();

                var taskCompletionSource = new TaskCompletionSource<Sprite>();
                
                await Task.Factory.StartNew(
                    () =>
                    {
                        using (var web = UnityWebRequestTexture.GetTexture(song.ArtUri, true))
                        {
                            var request = web.SendWebRequest();

                            request.completed += response =>
                            {
                                if (web.isNetworkError || web.isHttpError)
                                {
                                    taskCompletionSource.SetResult(null);
                                }
                                else
                                {
                                    var tex = DownloadHandlerTexture.GetContent(web);
                                    var result = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one * 0.5f, 100, 1);

                                    _songArtSpritCacheLock.EnterWriteLock();
                                    _songArtSpritCache[song.Id] = result;
                                    _songArtSpritCacheLock.ExitWriteLock();

                                    taskCompletionSource.SetResult(result);
                                }
                            };
                        }
                    },
                    TaskCreationOptions.AttachedToParent);

                return await taskCompletionSource.Task;
            }
        }
    }
}
