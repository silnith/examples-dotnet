using System.Data.Common;

namespace Silnith.CDB.SQLite;

public interface ICDBDatabase : IAsyncDisposable
{
    public Task ConnectAsync(DbConnectionStringBuilder connectionStringBuilder, CancellationToken cancellationToken = default);

    public Task CreateSchemaAsync(CancellationToken cancellationToken = default);
}
