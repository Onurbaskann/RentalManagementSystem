$renameMap = @{
    'KiraTakip/Services/Interfaces/IBelgeService.cs' = 'KiraTakip/Services/Interfaces/IDocumentService.cs'
    'KiraTakip/Services/Interfaces/IBirimService.cs' = 'KiraTakip/Services/Interfaces/IUnitService.cs'
    'KiraTakip/Services/Interfaces/IBorcHatirlatmaService.cs' = 'KiraTakip/Services/Interfaces/IChargeReminderService.cs'
    'KiraTakip/Services/Interfaces/IDavetiyeService.cs' = 'KiraTakip/Services/Interfaces/IInvitationService.cs'
    'KiraTakip/Services/Interfaces/IIstatistikService.cs' = 'KiraTakip/Services/Interfaces/IStatisticsService.cs'
    'KiraTakip/Services/Interfaces/IKiraciKullaniciService.cs' = 'KiraTakip/Services/Interfaces/ITenantUserService.cs'
    'KiraTakip/Services/Interfaces/IKiraciService.cs' = 'KiraTakip/Services/Interfaces/ITenantService.cs'
    'KiraTakip/Services/Interfaces/IManuelBorcService.cs' = 'KiraTakip/Services/Interfaces/IManualChargeService.cs'
    'KiraTakip/Services/Interfaces/IRolService.cs' = 'KiraTakip/Services/Interfaces/IRoleService.cs'
    'KiraTakip/Services/Interfaces/ISifreSifirlamaService.cs' = 'KiraTakip/Services/Interfaces/IPasswordResetService.cs'
    'KiraTakip/Services/Interfaces/ISozlesmeService.cs' = 'KiraTakip/Services/Interfaces/ILeaseService.cs'
    'KiraTakip/Services/Interfaces/ITarifeHiyerarsiService.cs' = 'KiraTakip/Services/Interfaces/IRateHierarchyService.cs'
    'KiraTakip/Services/Interfaces/ITasinmazFiyatService.cs' = 'KiraTakip/Services/Interfaces/IPropertyPricingService.cs'
    'KiraTakip/Services/Interfaces/ITasinmazService.cs' = 'KiraTakip/Services/Interfaces/IPropertyService.cs'
    'KiraTakip/Services/Interfaces/IUserRolService.cs' = 'KiraTakip/Services/Interfaces/IUserRoleService.cs'
    'KiraTakip/Services/Interfaces/IYetkiKapsamiCache.cs' = 'KiraTakip/Services/Interfaces/IPermissionScopeCache.cs'
    'KiraTakip/Services/Interfaces/IYetkiKapsamiProvider.cs' = 'KiraTakip/Services/Interfaces/IPermissionScopeProvider.cs'

    'KiraTakip/Services/BelgeService.cs' = 'KiraTakip/Services/DocumentService.cs'
    'KiraTakip/Services/BirimService.cs' = 'KiraTakip/Services/UnitService.cs'
    'KiraTakip/Services/BorcHatirlatmaService.cs' = 'KiraTakip/Services/ChargeReminderService.cs'
    'KiraTakip/Services/DavetiyeService.cs' = 'KiraTakip/Services/InvitationService.cs'
    'KiraTakip/Services/IstatistikService.cs' = 'KiraTakip/Services/StatisticsService.cs'
    'KiraTakip/Services/KiraciKullaniciService.cs' = 'KiraTakip/Services/TenantUserService.cs'
    'KiraTakip/Services/KiraciService.cs' = 'KiraTakip/Services/TenantService.cs'
    'KiraTakip/Services/ManuelBorcService.cs' = 'KiraTakip/Services/ManualChargeService.cs'
    'KiraTakip/Services/RolService.cs' = 'KiraTakip/Services/RoleService.cs'
    'KiraTakip/Services/SifreSifirlamaService.cs' = 'KiraTakip/Services/PasswordResetService.cs'
    'KiraTakip/Services/SozlesmeService.cs' = 'KiraTakip/Services/LeaseService.cs'
    'KiraTakip/Services/TarifeHiyerarsiService.cs' = 'KiraTakip/Services/RateHierarchyService.cs'
    'KiraTakip/Services/TasinmazFiyatService.cs' = 'KiraTakip/Services/PropertyPricingService.cs'
    'KiraTakip/Services/TasinmazService.cs' = 'KiraTakip/Services/PropertyService.cs'
    'KiraTakip/Services/UserRolService.cs' = 'KiraTakip/Services/UserRoleService.cs'
    'KiraTakip/Services/YetkiKapsamiCacheService.cs' = 'KiraTakip/Services/PermissionScopeCacheService.cs'
    'KiraTakip/Services/YetkiKapsamiProvider.cs' = 'KiraTakip/Services/PermissionScopeProvider.cs'
}

$keys = @(
    'IBelgeService', 'BelgeService', 'belgeService',
    'IBirimService', 'BirimService', 'birimService',
    'IBorcHatirlatmaService', 'BorcHatirlatmaService', 'borcHatirlatmaService',
    'IDavetiyeService', 'DavetiyeService', 'davetiyeService',
    'IIstatistikService', 'IstatistikService', 'istatistikService',
    'IKiraciKullaniciService', 'KiraciKullaniciService', 'kiraciKullaniciService',
    'IKiraciService', 'KiraciService', 'kiraciService',
    'IManuelBorcService', 'ManuelBorcService', 'manuelBorcService',
    'IRolService', 'RolService', 'rolService',
    'ISifreSifirlamaService', 'SifreSifirlamaService', 'sifreSifirlamaService',
    'ISozlesmeService', 'SozlesmeService', 'sozlesmeService',
    'ITarifeHiyerarsiService', 'TarifeHiyerarsiService', 'tarifeHiyerarsiService',
    'ITasinmazFiyatService', 'TasinmazFiyatService', 'tasinmazFiyatService',
    'ITasinmazService', 'TasinmazService', 'tasinmazService',
    'IUserRolService', 'UserRolService', 'userRolService',
    'IYetkiKapsamiCache', 'YetkiKapsamiCacheService', 'yetkiKapsamiCacheService', 'yetkiKapsamiCache',
    'IYetkiKapsamiProvider', 'YetkiKapsamiProvider', 'yetkiKapsamiProvider'
)

$vals = @(
    'IDocumentService', 'DocumentService', 'documentService',
    'IUnitService', 'UnitService', 'unitService',
    'IChargeReminderService', 'ChargeReminderService', 'chargeReminderService',
    'IInvitationService', 'InvitationService', 'invitationService',
    'IStatisticsService', 'StatisticsService', 'statisticsService',
    'ITenantUserService', 'TenantUserService', 'tenantUserService',
    'ITenantService', 'TenantService', 'tenantService',
    'IManualChargeService', 'ManualChargeService', 'manualChargeService',
    'IRoleService', 'RoleService', 'roleService',
    'IPasswordResetService', 'PasswordResetService', 'passwordResetService',
    'ILeaseService', 'LeaseService', 'leaseService',
    'IRateHierarchyService', 'RateHierarchyService', 'rateHierarchyService',
    'IPropertyPricingService', 'PropertyPricingService', 'propertyPricingService',
    'IPropertyService', 'PropertyService', 'propertyService',
    'IUserRoleService', 'UserRoleService', 'userRoleService',
    'IPermissionScopeCache', 'PermissionScopeCacheService', 'permissionScopeCacheService', 'permissionScopeCache',
    'IPermissionScopeProvider', 'PermissionScopeProvider', 'permissionScopeProvider'
)

$workspace = "d:\Software\RentalManagementSystem"

# Rename
foreach ($key in $renameMap.Keys) {
    $src = Join-Path $workspace ($key -replace '/', '\')
    $dest = Join-Path $workspace ($renameMap[$key] -replace '/', '\')
    if (Test-Path $src) {
        Write-Host "Renaming $src -> $dest"
        Move-Item -Path $src -Destination $dest -Force
    }
}

# Replace
$files = Get-ChildItem -Path "$workspace\KiraTakip", "$workspace\KiraTakip.Tests" -Recurse -Include *.cs, *.cshtml | Where-Object { 
    $_.FullName -notmatch '\\(obj|bin|Migrations|\.git|\.vs|node_modules)\\' 
}

foreach ($file in $files) {
    $content = [System.IO.File]::ReadAllText($file.FullName, [System.Text.Encoding]::UTF8)
    $modified = $false
    
    for ($i = 0; $i -lt $keys.Length; $i++) {
        $k = $keys[$i]
        $v = $vals[$i]
        $pattern = "\b$k\b"
        
        if ($content -cmatch $pattern) {
            $content = $content -creplace $pattern, $v
            $modified = $true
        }
    }
    
    if ($modified) {
        Write-Host "Updating $($file.FullName)"
        [System.IO.File]::WriteAllText($file.FullName, $content, [System.Text.Encoding]::UTF8)
    }
}

Write-Host "Refactoring Done!"
