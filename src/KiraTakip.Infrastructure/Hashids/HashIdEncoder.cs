using HashidsNet;
using KiraTakip.Common;

namespace KiraTakip.Infrastructure.Hashids;

public class HashIdEncoder(IHashids hashids) : IHashIdEncoder
{
    public string Encode(int id) => hashids.Encode(id);
}
