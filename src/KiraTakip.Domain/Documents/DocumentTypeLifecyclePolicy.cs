using KiraTakip.Models.Enums;

namespace KiraTakip.Domain.Documents;

public static class DocumentTypeLifecyclePolicy
{
    public static DocumentOwnerType ResolveTargetEntity(
        bool isSystem,
        DocumentOwnerType currentTarget,
        DocumentOwnerType requestedTarget)
        => isSystem ? currentTarget : requestedTarget;

    public static bool CanChangeActiveState(
        bool isSystem,
        bool currentIsActive,
        bool requestedIsActive)
        => !isSystem || !currentIsActive || requestedIsActive;

    public static bool CanDelete(bool isSystem)
        => !isSystem;
}