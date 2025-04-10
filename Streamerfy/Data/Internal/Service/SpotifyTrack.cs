namespace Streamerfy.Data.Internal.Service
{
    public class SpotifyTrack
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public string AlbumArtUrl { get; set; }
        public bool Explicit { get; set; }
        public SpotifyArtist Artist { get; set; }
    }
}
