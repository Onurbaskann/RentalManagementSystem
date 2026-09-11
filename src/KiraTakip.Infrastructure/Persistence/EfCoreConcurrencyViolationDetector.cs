using System;
using KiraTakip.Data;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Infrastructure.Persistence;

public class EfCoreConcurrencyViolationDetector : IConcurrencyViolationDetector
{
    public bool IsConcurrencyViolation(Exception exception)
    {
        return exception is DbUpdateConcurrencyException;
    }
}
