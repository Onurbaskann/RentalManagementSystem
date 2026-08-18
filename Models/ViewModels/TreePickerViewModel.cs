namespace KiraTakip.Models.ViewModels;

public sealed class TreePickerViewModel
{
    public string InputName { get; init; } = string.Empty;
    public IReadOnlyList<TreePickerNodeViewModel> Nodes { get; init; } = [];
    public string SearchPlaceholder { get; init; } = "Ara...";
    public string EmptyChildrenText { get; init; } = string.Empty;
    public string NoMatchesText { get; init; } = "Eşleşen kayıt bulunamadı.";
    public string EmptySelectionText { get; init; } = "Seçim yapılmadı";
    public string NodeCountLabel { get; init; } = "seçim";
    public string ChildCountLabel { get; init; } = "alt seçim";
    public string SummarySuffix { get; init; } = string.Empty;
    public string HighlightTitle { get; init; } = string.Empty;
}

public sealed record TreePickerNodeViewModel(
    string Value,
    string Label,
    string? GroupLabel,
    bool Selected,
    IReadOnlyList<TreePickerChildViewModel> Children);

public sealed record TreePickerChildViewModel(
    string Value,
    string Label,
    bool Selected,
    bool Highlighted = false);
