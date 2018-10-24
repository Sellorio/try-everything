Try Everything! is a light-weight mod which lets you easily try out the latest songs without going through the hassle of downloading each new song from BeatSaver manually and then deleting the ones you don't like.

#### What does it do

This mod downloads the 5 latest songs from BeatSaver and loads them up. Once you have completed a song (success or fail) you will be given the option to "Keep" it, "Reject" it or "Blacklist Author" to never see another song by that author. You also have the option to open the song in bsaber.com (BeastSaber) if you want to review it.

_Note: Some songs do not have a BeastSaber page and so the BeastSaber home page will load._

Once you pick from one of the 3 options, the mod will download another song (you will always have 5 pending songs). Due to issues with the Song Loader mod (I'm currently trying to reach the Author), you will need to go out of the song list and back in to refresh the list with the latest downloads.

#### How to install

Just put the dll in the Plugins folder and you're done!

#### Issues with saved data

All data is saved in plain text files in the UserData folder. The 3 files used are:

* `TryEverything.PendingSongs.txt`<br>A list of the currently pending songs - songs awaiting your decision: Keep, Reject, Blacklist Author.
* `TryEverything.RejectedSongs.txt`<br>A list of songs that have been rejected. This file's data ensures that the mod will not re-download a song you have rejected.
* `TryEverything.IgnoredAuthors.txt`<br>A list of BeatSaver usernames that will be ignored when searching for the next song to download.

#### Upcoming features (we hope)

* Hooking into the Delete button that (I think?) is provided by Song Loader (might be BeatSaver Downloader) and making it also reject the song if it is a pending song.
