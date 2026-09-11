namespace KiraTakip.Data;

public interface IUniqueConstraintViolationDetector
{
    bool IsUniqueConstraintViolation(Exception exception);
}
