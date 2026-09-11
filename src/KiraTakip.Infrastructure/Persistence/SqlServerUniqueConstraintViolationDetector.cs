using System;
using KiraTakip.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Infrastructure.Persistence;

public class SqlServerUniqueConstraintViolationDetector : IUniqueConstraintViolationDetector
{
    public bool IsUniqueConstraintViolation(Exception exception)
    {
        if (exception is DbUpdateException dbUpdateEx && dbUpdateEx.InnerException is SqlException sqlEx)
        {
            return sqlEx.Number is 2601 or 2627;
        }

        return false;
    }
}
