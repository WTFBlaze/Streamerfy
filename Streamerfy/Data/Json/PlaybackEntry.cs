namespace Streamerfy.Data.Internal.Json
{
    public class PlaybackEntry
    {
        public string TrackId { get; set; }
        public string TrackName { get; set; }
        public string ArtistName { get; set; }
        public string ArtistId { get; set; }
        public string AlbumArtUrl { get; set; }
        public bool IsExplicit { get; set; }
        public DateTime Timestamp { get; set; }
        public string RequestedBy { get; set; } = "Unknown";
    }
}
