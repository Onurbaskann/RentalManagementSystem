namespace KiraTakip.Infrastructure.Transactions;

/// <summary>
/// Bir servis sınıfının tüm public async metotlarının otomatik olarak
/// veritabanı transaction'ı içinde çalıştırılmasını sağlar.
/// Bu interface'i implement eden servisler DI tarafında otomatik proxy ile sarılır.
/// Metot başarıyla tamamlanırsa commit, exception fırlatılırsa rollback yapılır.
/// İç içe çağrılar (nested) algılanır; aynı transaction'a join eder, çift commit olmaz.
/// </summary>
public interface ITransactionalService
{
}
