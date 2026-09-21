namespace KiraTakip.Infrastructure.Transactions;

/// <summary>
/// Yalnız işaretlenen servis metodunu veritabanı transaction'ı içinde çalıştırır.
/// E-posta ve diğer dış sistem işlemleri içeren metotların tamamını transaction'a
/// almak yerine dar bir transaction sınırı tanımlamak için kullanılır.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public sealed class TransactionalAttribute : Attribute;
