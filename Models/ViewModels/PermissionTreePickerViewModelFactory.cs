namespace KiraTakip.Models.ViewModels;

public static class PermissionTreePickerViewModelFactory
{
    private static readonly HashSet<string> HighlightedActionSegments =
    [
        "Delete",
        "DeleteDraft",
        "Cancel",
        "Terminate",
        "OverrideRate",
        "Approve",
        "Reject"
    ];

    public static TreePickerViewModel Create(
        IReadOnlyCollection<PermissionGroupViewModel> permissionGroups)
        => new()
        {
            InputName = "SelectedPermissions",
            SearchPlaceholder = "İzin ara...",
            EmptyChildrenText = "yalnızca görüntüleme",
            NoMatchesText = "Eşleşen izin bulunamadı.",
            EmptySelectionText = "İzin seçilmedi",
            NodeCountLabel = "modül",
            ChildCountLabel = "eylem",
            SummarySuffix = "atanmış",
            HighlightTitle = "Kritik işlem",
            Nodes = permissionGroups.Select(CreateNode).ToList()
        };

    private static TreePickerNodeViewModel CreateNode(PermissionGroupViewModel group)
    {
        var modulePermission = group.Permissions.FirstOrDefault();
        return new TreePickerNodeViewModel(
            modulePermission?.Value ?? string.Empty,
            group.GroupName,
            group.ParentGroupName,
            modulePermission?.Selected ?? false,
            group.Permissions
                .Skip(1)
                .Select(permission => new TreePickerChildViewModel(
                    permission.Value,
                    permission.Label,
                    permission.Selected,
                    IsHighlighted(permission.Value)))
                .ToList());
    }

    private static bool IsHighlighted(string permission)
        => HighlightedActionSegments.Contains(
            permission.Split('.').LastOrDefault() ?? string.Empty);
}
