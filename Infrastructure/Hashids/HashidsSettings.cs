namespace KiraTakip.Infrastructure.Hashids
{
    public class HashidsSettings
    {
        public string Salt { get; set; } = string.Empty;
        public int MinHashLength { get; set; } = 6;
    }
}
