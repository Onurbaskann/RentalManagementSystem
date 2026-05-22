namespace KiraTakip.Data;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _ctx;

    public UnitOfWork(ApplicationDbContext ctx) => _ctx = ctx;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _ctx.SaveChangesAsync(cancellationToken);
}
