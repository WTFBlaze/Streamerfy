using Newtonsoft.Json;
using Streamerfy.Data.Internal.Json;
using Streamerfy.Data.Internal.Service;
using System.IO;
using System.Text.Json;

namespace Streamerfy.Services
{
    public class PlaybackHistoryService
    {
        private readonly List<PlaybackEntry> _entries = new();

        public IReadOnlyList<PlaybackEntry> Entries => _entries.AsReadOnly();

        public PlaybackHistoryService()
        {
            Load();
        }

        public void Add(SpotifyTrack track, string requestedBy = "Streamer")
        {
            var entry = new PlaybackEntry
            {
                TrackId = track.ID,
                TrackName = track.Name,
                ArtistName = track.Artist.Name,
                ArtistId = track.Artist.ID,
                AlbumArtUrl = track.AlbumArtUrl,
                IsExplicit = track.Explicit,
                Timestamp = DateTime.UtcNow,
                RequestedBy = requestedBy
            };
            _entries.Add(entry);
            Save();
        }

        public void Clear()
        {
            _entries.Clear();
            Save();
        }


        #region Internal Methods
        private void Load()
        {
            if (File.Exists(App.PlaybackFile))
            {
                var json = File.ReadAllText(App.PlaybackFile);
                var list = JsonConvert.DeserializeObject<List<PlaybackEntry>>(json);
                if (list != null)
                    _entries.AddRange(list);
            }
        }

        private void Save()
        {
            var json = JsonConvert.SerializeObject(_entries, Formatting.Indented);
            File.WriteAllText(App.PlaybackFile, json);
        }
        #endregion
    }
}
