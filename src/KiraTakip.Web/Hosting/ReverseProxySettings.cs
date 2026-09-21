namespace KiraTakip.Web.Hosting;

public sealed class ReverseProxySettings
{
    public const string SectionName = "ReverseProxy";

    public bool Enabled { get; set; }
    public int ForwardLimit { get; set; } = 1;
    public List<string> KnownProxies { get; set; } = [];
}
