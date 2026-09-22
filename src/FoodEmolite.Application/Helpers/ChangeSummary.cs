using System.Globalization;

namespace FoodEmolite.Application.Helpers;

/// <summary>
/// Gom các thay đổi (trước → sau) của 1 lần cập nhật để ghi vào mô tả nhật ký hoạt động,
/// ví dụ: Cập nhật món "Cà phê": Giá: 20.000đ → 25.000đ; Trạng thái: Đang bán → Ngừng bán.
/// </summary>
public sealed class ChangeSummary
{
    private const int MaxTextLength = 60;

    private readonly List<string> _changes = new();

    public bool HasChanges => _changes.Count > 0;

    public ChangeSummary Text(string label, string? oldValue, string? newValue)
    {
        var oldText = Normalize(oldValue);
        var newText = Normalize(newValue);

        if (oldText != newText)
            _changes.Add($"{label}: {Show(oldText)} → {Show(newText)}");

        return this;
    }

    public ChangeSummary Money(string label, decimal? oldValue, decimal? newValue)
    {
        if (oldValue != newValue)
            _changes.Add($"{label}: {FormatMoney(oldValue)} → {FormatMoney(newValue)}");

        return this;
    }

    public ChangeSummary Number(string label, long? oldValue, long? newValue)
    {
        if (oldValue != newValue)
            _changes.Add($"{label}: {oldValue?.ToString(CultureInfo.InvariantCulture) ?? "(trống)"} → {newValue?.ToString(CultureInfo.InvariantCulture) ?? "(trống)"}");

        return this;
    }

    public ChangeSummary Flag(string label, bool oldValue, bool newValue, string trueText, string falseText)
    {
        if (oldValue != newValue)
            _changes.Add($"{label}: {(oldValue ? trueText : falseText)} → {(newValue ? trueText : falseText)}");

        return this;
    }

    /// <summary>Thay đổi không so sánh trước/sau được (đổi ảnh, thêm/xoá mục con...).</summary>
    public ChangeSummary Note(string text)
    {
        _changes.Add(text);

        return this;
    }

    /// <summary>Ghép tiền tố + danh sách thay đổi; không có gì đổi thì ghi rõ để người xem không phải đoán.</summary>
    public string Describe(string prefix)
    {
        return HasChanges
            ? $"{prefix}: {string.Join("; ", _changes)}"
            : $"{prefix} (không thay đổi nội dung)";
    }

    private static string Normalize(string? value) => value?.Trim() ?? string.Empty;

    private static string Show(string value)
    {
        if (value.Length == 0)
            return "(trống)";

        return value.Length > MaxTextLength
            ? value[..MaxTextLength] + "…"
            : value;
    }

    private static string FormatMoney(decimal? value)
        => value.HasValue ? $"{value.Value:N0}đ" : "(trống)";
}
