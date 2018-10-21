using System.Threading.Tasks;
using TryEverything.Data;
using UnityEngine;

namespace TryEverything.Services
{
    interface ISongLoadingService
    {
        Task<Sprite> LoadArt(CustomSong song);
    }
}
