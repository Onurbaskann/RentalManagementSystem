import os
import re

workspace_dir = r"d:\Software\RentalManagementSystem"
kira_takip_dir = os.path.join(workspace_dir, "KiraTakip")
tests_dir = os.path.join(workspace_dir, "KiraTakip.Tests")

# 1. Rename files map
rename_map = {
    # Interfaces
    r"KiraTakip/Repositories/Interfaces/IBelgeTuruRepository.cs": r"KiraTakip/Repositories/Interfaces/IDocumentTypeRepository.cs",
    r"KiraTakip/Repositories/Interfaces/IBirimRepository.cs": r"KiraTakip/Repositories/Interfaces/IUnitRepository.cs",
    r"KiraTakip/Repositories/Interfaces/IBorcTipiRepository.cs": r"KiraTakip/Repositories/Interfaces/IChargeTypeRepository.cs",
    r"KiraTakip/Repositories/Interfaces/IKiraciRepository.cs": r"KiraTakip/Repositories/Interfaces/ITenantRepository.cs",
    r"KiraTakip/Repositories/Interfaces/ISozlesmeRepository.cs": r"KiraTakip/Repositories/Interfaces/ILeaseRepository.cs",
    r"KiraTakip/Repositories/Interfaces/ITasinmazRepository.cs": r"KiraTakip/Repositories/Interfaces/IPropertyRepository.cs",
    r"KiraTakip/Repositories/Interfaces/IBirimTuruRepository.cs": r"KiraTakip/Repositories/Interfaces/IUnitTypeRepository.cs",
    # Concrete Repositories
    r"KiraTakip/Repositories/BelgeTuruRepository.cs": r"KiraTakip/Repositories/DocumentTypeRepository.cs",
    r"KiraTakip/Repositories/BirimRepository.cs": r"KiraTakip/Repositories/UnitRepository.cs",
    r"KiraTakip/Repositories/BorcTipiRepository.cs": r"KiraTakip/Repositories/ChargeTypeRepository.cs",
    r"KiraTakip/Repositories/KiraciRepository.cs": r"KiraTakip/Repositories/TenantRepository.cs",
    r"KiraTakip/Repositories/SozlesmeRepository.cs": r"KiraTakip/Repositories/LeaseRepository.cs",
    r"KiraTakip/Repositories/TasinmazRepository.cs": r"KiraTakip/Repositories/PropertyRepository.cs",
    r"KiraTakip/Repositories/BirimTuruRepository.cs": r"KiraTakip/Repositories/UnitTypeRepository.cs",
}

# 2. Replacements dictionary (ordered by length descending to prevent substring collisions)
replacements = {
    # Interfaces & Classes
    "IBelgeTuruRepository": "IDocumentTypeRepository",
    "BelgeTuruRepository": "DocumentTypeRepository",
    "belgeTuruRepository": "documentTypeRepository",
    
    "IBirimRepository": "IUnitRepository",
    "BirimRepository": "UnitRepository",
    "birimRepository": "unitRepository",
    
    "IBorcTipiRepository": "IChargeTypeRepository",
    "BorcTipiRepository": "ChargeTypeRepository",
    "borcTipiRepository": "chargeTypeRepository",
    
    "IKiraciRepository": "ITenantRepository",
    "KiraciRepository": "TenantRepository",
    "kiraciRepository": "tenantRepository",
    
    "ISozlesmeRepository": "ILeaseRepository",
    "SozlesmeRepository": "LeaseRepository",
    "sozlesmeRepository": "leaseRepository",
    
    "ITasinmazRepository": "IPropertyRepository",
    "TasinmazRepository": "PropertyRepository",
    "tasinmazRepository": "propertyRepository",
    
    "IBirimTuruRepository": "IUnitTypeRepository",
    "BirimTuruRepository": "UnitTypeRepository",
    "birimTuruRepository": "unitTypeRepository",
    
    # Method names
    "GetByTasinmazIdAsync": "GetByPropertyIdAsync",
    "GetTasinmazIdAsync": "GetPropertyIdAsync",
    "GetExistingKiraciNosAsync": "GetExistingTenantNosAsync",
    "GetByKiraciIdAsync": "GetByTenantIdAsync",
    "GetByBirimIdAsync": "GetByUnitIdAsync",
    "GetTasinmazVeKategoriAsync": "GetPropertyAndCategoryAsync",
    
    # Parameters & Variables
    "yetkiliTasinmazIds": "yetkiliPropertyIds",
    "tasinmazId": "propertyId",
    "kiraciId": "tenantId",
    "sozlesmeId": "leaseId",
    "birimId": "unitId",
    "borcTipiId": "chargeTypeId",
    "birimTuruId": "unitTypeId",
    "sozlesmeIds": "leaseIds",
}

# Rename the files
for old_rel, new_rel in rename_map.items():
    old_path = os.path.join(workspace_dir, old_rel.replace('/', os.sep))
    new_path = os.path.join(workspace_dir, new_rel.replace('/', os.sep))
    if os.path.exists(old_path):
        print(f"Renaming {old_path} -> {new_path}")
        os.rename(old_path, new_path)
    else:
        print(f"File not found to rename: {old_path}")

# Run replacements in all .cs and .cshtml files
for root_dir in [kira_takip_dir, tests_dir]:
    if not os.path.exists(root_dir):
        continue
        
    for root, dirs, files in os.walk(root_dir):
        # Exclude directories we shouldn't touch
        excluded_dirs = ['bin', 'obj', 'Migrations', '.git', '.vs', 'node_modules']
        dirs[:] = [d for d in dirs if d not in excluded_dirs]
            
        for file in files:
            if file.endswith('.cs') or file.endswith('.cshtml'):
                filepath = os.path.join(root, file)
                
                # Read with utf-8-sig to automatically handle and strip BOM if present
                try:
                    with open(filepath, 'r', encoding='utf-8-sig') as f:
                        content = f.read()
                except UnicodeDecodeError:
                    # If it's still failing, fallback to local windows encoding just in case, but warn
                    print(f"Warning: UnicodeDecodeError on {filepath}, trying windows-1254")
                    with open(filepath, 'r', encoding='windows-1254') as f:
                        content = f.read()
                
                modified = False
                for search_str, replace_str in replacements.items():
                    # Use word boundaries for replacement
                    pattern = r'\b' + re.escape(search_str) + r'\b'
                    # re.subn returns a tuple (new_string, number_of_subs_made)
                    new_content, count = re.subn(pattern, replace_str, content)
                    if count > 0:
                        content = new_content
                        modified = True
                
                if modified:
                    print(f"Updating {filepath}")
                    # Write back with utf-8-sig to preserve BOM (standard for Visual Studio)
                    with open(filepath, 'w', encoding='utf-8-sig') as f:
                        f.write(content)

print("Finished Phase 4 refactoring updates successfully!")
