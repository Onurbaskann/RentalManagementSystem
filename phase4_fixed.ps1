$renameMap = @{
    'KiraTakip/Repositories/Interfaces/IBelgeTuruRepository.cs' = 'KiraTakip/Repositories/Interfaces/IDocumentTypeRepository.cs'
    'KiraTakip/Repositories/Interfaces/IBirimRepository.cs' = 'KiraTakip/Repositories/Interfaces/IUnitRepository.cs'
    'KiraTakip/Repositories/Interfaces/IBorcTipiRepository.cs' = 'KiraTakip/Repositories/Interfaces/IChargeTypeRepository.cs'
    'KiraTakip/Repositories/Interfaces/IKiraciRepository.cs' = 'KiraTakip/Repositories/Interfaces/ITenantRepository.cs'
    'KiraTakip/Repositories/Interfaces/ISozlesmeRepository.cs' = 'KiraTakip/Repositories/Interfaces/ILeaseRepository.cs'
    'KiraTakip/Repositories/Interfaces/ITasinmazRepository.cs' = 'KiraTakip/Repositories/Interfaces/IPropertyRepository.cs'
    'KiraTakip/Repositories/Interfaces/IBirimTuruRepository.cs' = 'KiraTakip/Repositories/Interfaces/IUnitTypeRepository.cs'
    
    'KiraTakip/Repositories/BelgeTuruRepository.cs' = 'KiraTakip/Repositories/DocumentTypeRepository.cs'
    'KiraTakip/Repositories/BirimRepository.cs' = 'KiraTakip/Repositories/UnitRepository.cs'
    'KiraTakip/Repositories/BorcTipiRepository.cs' = 'KiraTakip/Repositories/ChargeTypeRepository.cs'
    'KiraTakip/Repositories/KiraciRepository.cs' = 'KiraTakip/Repositories/TenantRepository.cs'
    'KiraTakip/Repositories/SozlesmeRepository.cs' = 'KiraTakip/Repositories/LeaseRepository.cs'
    'KiraTakip/Repositories/TasinmazRepository.cs' = 'KiraTakip/Repositories/PropertyRepository.cs'
    'KiraTakip/Repositories/BirimTuruRepository.cs' = 'KiraTakip/Repositories/UnitTypeRepository.cs'
}

$keys = @(
    'IBelgeTuruRepository', 'BelgeTuruRepository', 'belgeTuruRepository',
    'IBirimRepository', 'BirimRepository', 'birimRepository',
    'IBorcTipiRepository', 'BorcTipiRepository', 'borcTipiRepository',
    'IKiraciRepository', 'KiraciRepository', 'kiraciRepository',
    'ISozlesmeRepository', 'SozlesmeRepository', 'sozlesmeRepository',
    'ITasinmazRepository', 'TasinmazRepository', 'tasinmazRepository',
    'IBirimTuruRepository', 'BirimTuruRepository', 'birimTuruRepository',
    'GetByTasinmazIdAsync', 'GetTasinmazIdAsync', 'GetExistingKiraciNosAsync',
    'GetByKiraciIdAsync', 'GetByBirimIdAsync', 'GetTasinmazVeKategoriAsync',
    'yetkiliTasinmazIds', 'tasinmazId', 'kiraciId', 'sozlesmeId', 'birimId',
    'borcTipiId', 'birimTuruId', 'sozlesmeIds'
)
$vals = @(
    'IDocumentTypeRepository', 'DocumentTypeRepository', 'documentTypeRepository',
    'IUnitRepository', 'UnitRepository', 'unitRepository',
    'IChargeTypeRepository', 'ChargeTypeRepository', 'chargeTypeRepository',
    'ITenantRepository', 'TenantRepository', 'tenantRepository',
    'ILeaseRepository', 'LeaseRepository', 'leaseRepository',
    'IPropertyRepository', 'PropertyRepository', 'propertyRepository',
    'IUnitTypeRepository', 'UnitTypeRepository', 'unitTypeRepository',
    'GetByPropertyIdAsync', 'GetPropertyIdAsync', 'GetExistingTenantNosAsync',
    'GetByTenantIdAsync', 'GetByUnitIdAsync', 'GetPropertyAndCategoryAsync',
    'yetkiliPropertyIds', 'propertyId', 'tenantId', 'leaseId', 'unitId',
    'chargeTypeId', 'unitTypeId', 'leaseIds'
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
    # Read as UTF8 text
    $content = [System.IO.File]::ReadAllText($file.FullName, [System.Text.Encoding]::UTF8)
    $modified = $false
    
    for ($i = 0; $i -lt $keys.Length; $i++) {
        $k = $keys[$i]
        $v = $vals[$i]
        $pattern = "\b$k\b"
        
        # -cmatch is case-sensitive
        if ($content -cmatch $pattern) {
            $content = $content -creplace $pattern, $v
            $modified = $true
        }
    }
    
    if ($modified) {
        Write-Host "Updating $($file.FullName)"
        # Write back preserving UTF8 BOM
        [System.IO.File]::WriteAllText($file.FullName, $content, [System.Text.Encoding]::UTF8)
    }
}

Write-Host "Refactoring Done!"
