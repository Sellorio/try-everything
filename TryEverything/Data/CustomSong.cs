using System;

namespace TryEverything.Data
{
    class CustomSong
    {
        public string Id { get; }
        public string Title { get; } // songName
        public string SubTitle { get; } // songSubName
        public string AuthorName { get; } // songAuthorName
        public float BeatsPerMinute { get; }
        public int Upvotes { get; }
        public int Downvotes { get; }
        public Uri DownloadUri { get; }
        public Uri ArtUri { get; }
        public DifficultyLevels DifficultyLevels { get; }

        public CustomSong(
            string id,
            string title,
            string subTitle,
            string authorName,
            float beatsPerMinute,
            int upvotes,
            int downvotes,
            Uri downloadUri,
            Uri artUri,
            DifficultyLevels difficultyLevels)
        {
            Id = id;
            Title = title;
            SubTitle = subTitle;
            AuthorName = authorName;
            BeatsPerMinute = beatsPerMinute;
            Upvotes = upvotes;
            Downvotes = downvotes;
            DownloadUri = downloadUri;
            ArtUri = artUri;
            DifficultyLevels = difficultyLevels;
        }
    }
}
