namespace TNA.BLL.Config
{
    public class PubgOptions
    {
        public string ApiKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://api.pubg.com/shards/steam";
    }
}
