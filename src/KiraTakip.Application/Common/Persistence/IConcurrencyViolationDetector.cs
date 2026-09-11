using System;

namespace KiraTakip.Data;

public interface IConcurrencyViolationDetector
{
    bool IsConcurrencyViolation(Exception exception);
}
