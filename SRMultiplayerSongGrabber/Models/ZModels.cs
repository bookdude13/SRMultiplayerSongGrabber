using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayerSongGrabber.Models
{
    public class BeatmapList
    {
        public BeatmapInfoZ[] Property1 { get; set; }
    }

    public class BeatmapInfoZ
    {
        public int id { get; set; }
        public string hash { get; set; }
        public string title { get; set; }
        public string artist { get; set; }
        public string mapper { get; set; }
        public string duration { get; set; }
        public string bpm { get; set; }
        public string[] difficulties { get; set; }
        public string description { get; set; }
        public string youtube_url { get; set; }
        public string filename { get; set; }
        public string filename_original { get; set; }
        public int cover_version { get; set; }
        public int play_count { get; set; }
        public int play_count_daily { get; set; }
        public int download_count { get; set; }
        public int upvote_count { get; set; }
        public int downvote_count { get; set; }
        public int vote_diff { get; set; }
        public string score { get; set; }
        public string rating { get; set; }
        public bool published { get; set; }
        public bool production_mode { get; set; }
        public bool beat_saber_convert { get; set; }
        public bool _explicit { get; set; }
        public bool ost { get; set; }
        public DateTime published_at { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public int version { get; set; }
        public User user { get; set; }
        public object[] collaborators { get; set; }
        public string download_url { get; set; }
        public string cover_url { get; set; }
        public string preview_url { get; set; }
        public string video_url { get; set; }
    }

    public class User
    {
        public int id { get; set; }
        public string username { get; set; }
        public string avatar_filename { get; set; }
        public string avatar_url { get; set; }
    }
}
