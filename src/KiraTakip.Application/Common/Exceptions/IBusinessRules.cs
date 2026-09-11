namespace KiraTakip.Infrastructure.Exceptions;

/// <summary>
/// Domain'e özel iş kuralı interface'lerinin (örn. ILeaseBusinessRules) türediği
/// işaretleyici (marker) arayüz. BusinessRulesModule, bu arayüzden türeyen interface'leri
/// tarayıp tek implementasyonlarını DI'a kaydeder.
///
/// İsim tabanlı (suffix) taramanın aksine: IBusinessRules yanlış yazılırsa/var olmayan bir
/// tipe atıfta bulunulursa derleme hatası alınır — isim eşleşmesi sessizce atlanmaz.
/// </summary>
public interface IBusinessRules
{
}
