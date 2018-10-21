using System.Collections.Generic;
using TryEverything.Data;

namespace TryEverything.Services
{
    interface IBeatSaverService
    {
        /// <summary>
        /// Downloads the specified song from Beat Saver and extracts it to the CustomSongs directory.
        /// The song still needs to be loaded for it to be available without restarting the game.
        /// </summary>
        /// <param name="song">The song to download.</param>
        /// <returns>The task for the current work.</returns>
        System.Collections.IEnumerator DownloadSong(CustomSong song);

        /// <summary>
        /// Retrieves the set of the latest songs.
        /// </summary>
        /// <param name="page">The page of songs to retrieve.</param>
        /// <returns>The list of songs in the specified page.</returns>
        IEnumerable<IEnumerable<CustomSong>> GetSongs(int page = 1);

        /// <summary>
        /// Retrieves a song by it's level id. This is the only way to reliably get song details from
        /// the reivew screen.
        /// </summary>
        /// <param name="levelId">The level id of the song.</param>
        /// <returns>The matching song or null if not found.</returns>
        IEnumerable<CustomSong> GetSongFromLevel(string levelId);
    }
}
