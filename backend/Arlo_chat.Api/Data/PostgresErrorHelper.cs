using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Arlo_chat.Api.Data;

public static class PostgresErrorHelper
{
    public static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
