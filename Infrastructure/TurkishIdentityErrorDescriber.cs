using Microsoft.AspNetCore.Identity;

namespace KiraTakip.Infrastructure;

public class TurkishIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError DefaultError() => new() { Code = nameof(DefaultError), Description = "Bilinmeyen bir hata oluştu." };
    
    public override IdentityError ConcurrencyFailure() => new() { Code = nameof(ConcurrencyFailure), Description = "Bu veri daha önce güncellenmiş. Lütfen sayfayı yenileyip tekrar deneyin." };
    
    public override IdentityError PasswordMismatch() => new() { Code = nameof(PasswordMismatch), Description = "Mevcut şifreniz hatalı." };
    
    public override IdentityError InvalidToken() => new() { Code = nameof(InvalidToken), Description = "Geçersiz güvenlik kodu." };
    
    public override IdentityError LoginAlreadyAssociated() => new() { Code = nameof(LoginAlreadyAssociated), Description = "Bu kullanıcı adı zaten başka bir hesapla ilişkilendirilmiş." };
    
    public override IdentityError InvalidUserName(string? userName) => new() { Code = nameof(InvalidUserName), Description = $"'{userName}' kullanıcı adı geçersizdir. Yalnızca harf ve rakam içerebilir." };
    
    public override IdentityError InvalidEmail(string? email) => new() { Code = nameof(InvalidEmail), Description = $"'{email}' e-posta adresi geçersizdir." };
    
    public override IdentityError DuplicateUserName(string userName) => new() { Code = nameof(DuplicateUserName), Description = $"'{userName}' kullanıcı adı zaten kullanımda." };
    
    public override IdentityError DuplicateEmail(string email) => new() { Code = nameof(DuplicateEmail), Description = $"'{email}' e-posta adresi zaten başka bir kullanıcı tarafından kullanılıyor." };
    
    public override IdentityError InvalidRoleName(string? role) => new() { Code = nameof(InvalidRoleName), Description = $"'{role}' rol ismi geçersizdir." };
    
    public override IdentityError DuplicateRoleName(string role) => new() { Code = nameof(DuplicateRoleName), Description = $"'{role}' rolü zaten mevcut." };
    
    public override IdentityError UserAlreadyHasPassword() => new() { Code = nameof(UserAlreadyHasPassword), Description = "Kullanıcının zaten bir şifresi var." };
    
    public override IdentityError UserLockoutNotEnabled() => new() { Code = nameof(UserLockoutNotEnabled), Description = "Bu kullanıcı için kilitlenme aktif değil." };
    
    public override IdentityError UserAlreadyInRole(string role) => new() { Code = nameof(UserAlreadyInRole), Description = $"Kullanıcı zaten '{role}' rolünde." };
    
    public override IdentityError UserNotInRole(string role) => new() { Code = nameof(UserNotInRole), Description = $"Kullanıcı '{role}' rolünde değil." };
    
    public override IdentityError PasswordTooShort(int length) => new() { Code = nameof(PasswordTooShort), Description = $"Şifre en az {length} karakter uzunluğunda olmalıdır." };
    
    public override IdentityError PasswordRequiresNonAlphanumeric() => new() { Code = nameof(PasswordRequiresNonAlphanumeric), Description = "Şifre en az bir özel karakter (noktalama işareti veya simge) içermelidir." };
    
    public override IdentityError PasswordRequiresDigit() => new() { Code = nameof(PasswordRequiresDigit), Description = "Şifre en az bir rakam ('0'-'9') içermelidir." };
    
    public override IdentityError PasswordRequiresLower() => new() { Code = nameof(PasswordRequiresLower), Description = "Şifre en az bir küçük harf ('a'-'z') içermelidir." };
    
    public override IdentityError PasswordRequiresUpper() => new() { Code = nameof(PasswordRequiresUpper), Description = "Şifre en az bir büyük harf ('A'-'Z') içermelidir." };
    
    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) => new() { Code = nameof(PasswordRequiresUniqueChars), Description = $"Şifre en az {uniqueChars} farklı karakter içermelidir." };
}
