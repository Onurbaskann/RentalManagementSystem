using System.Globalization;

namespace KiraTakip.Models.Settings;

public enum SystemSettingInputKind
{
    Integer,
    Text
}

public sealed record SystemSettingDefinition(
    int SeedId,
    string Key,
    string DisplayName,
    string GroupDisplayName,
    string Description,
    string DefaultValue,
    SystemSettingInputKind InputKind,
    bool IsEditable,
    int? MinimumValue = null,
    int? MaximumValue = null);

public static class SystemSettingDefinitions
{
    public static class Reservation
    {
        public const string MinimumDurationMinutes = "Reservation.MinimumDurationMinutes";
        public const string MaximumDurationMinutes = "Reservation.MaximumDurationMinutes";
        public const string MinimumAdvanceMinutes = "Reservation.MinimumAdvanceMinutes";
        public const string MaximumAdvanceDays = "Reservation.MaximumAdvanceDays";
        public const string ModificationCutoffMinutes = "Reservation.ModificationCutoffMinutes";
        public const string CompletionGraceMinutes = "Reservation.CompletionGraceMinutes";
        public const string MaximumAttendeeCount = "Reservation.MaximumAttendeeCount";
    }

    public static class Payment
    {
        public const string LinkValidityHours = "Payment.LinkValidityHours";
        public const string ReminderDaysBefore = "Payment.ReminderDaysBefore";
        public const string ReminderCooldownDays = "Payment.ReminderCooldownDays";
    }

    public static class Invitation
    {
        public const string ValidityDays = "Invitation.ValidityDays";
        public const string ResendCooldownMinutes = "Invitation.ResendCooldownMinutes";
    }

    public static class Lease
    {
        public const string ExpiringSoonStatusDays = "Lease.ExpiringSoonStatusDays";
    }

    public static class Dashboard
    {
        public const string ExpiringLeaseLookaheadDays = "Dashboard.ExpiringLeaseLookaheadDays";
    }

    public static class BankMatching
    {
        public const string AmountTolerancePercent = "BankMatching.AmountTolerancePercent";
        public const string DateToleranceDays = "BankMatching.DateToleranceDays";
    }

    public static readonly IReadOnlyList<SystemSettingDefinition> All =
    [
        new(1, Reservation.MinimumDurationMinutes, "Minimum rezervasyon süresi", "Rezervasyon",
            "Bir rezervasyonun dakika cinsinden en kısa süresi.", "15",
            SystemSettingInputKind.Integer, true, 1, 1440),
        new(2, Reservation.MaximumDurationMinutes, "Maksimum rezervasyon süresi", "Rezervasyon",
            "Bir rezervasyonun dakika cinsinden en uzun süresi.", "1440",
            SystemSettingInputKind.Integer, true, 1, 10080),
        new(3, Reservation.MinimumAdvanceMinutes, "Minimum ileri tarih süresi", "Rezervasyon",
            "Rezervasyon başlangıcının mevcut zamandan en az kaç dakika sonra olacağı.", "0",
            SystemSettingInputKind.Integer, true, 0, 10080),
        new(4, Reservation.MaximumAdvanceDays, "Maksimum ileri tarih günü", "Rezervasyon",
            "En fazla kaç gün sonrası için rezervasyon oluşturulabileceği.", "365",
            SystemSettingInputKind.Integer, true, 1, 3650),
        new(5, Reservation.ModificationCutoffMinutes, "Değişiklik ve iptal sınırı", "Rezervasyon",
            "Başlangıca bu süreden az kaldığında normal güncelleme ve iptalin engelleneceği dakika.", "120",
            SystemSettingInputKind.Integer, true, 0, 10080),
        new(6, Reservation.CompletionGraceMinutes, "Otomatik tamamlama bekleme süresi", "Rezervasyon",
            "Bitişten kaç dakika sonra rezervasyonun tamamlanmış sayılacağı.", "15",
            SystemSettingInputKind.Integer, true, 0, 1440),
        new(7, Reservation.MaximumAttendeeCount, "Maksimum katılımcı sayısı", "Rezervasyon",
            "Rezervasyona eklenebilecek en fazla katılımcı sayısı.", "100",
            SystemSettingInputKind.Integer, true, 1, 10000),
        new(8, Payment.LinkValidityHours, "Ödeme bağlantısı geçerlilik süresi", "Ödeme ve Hatırlatma",
            "Ödeme bağlantısının kaç saat geçerli kalacağı.", "168",
            SystemSettingInputKind.Integer, true, 1, 8760),
        new(9, Payment.ReminderDaysBefore, "Hatırlatma başlangıç günü", "Ödeme ve Hatırlatma",
            "Vade tarihinden kaç gün önce borcun hatırlatma kapsamına alınacağı.", "5",
            SystemSettingInputKind.Integer, true, 0, 365),
        new(10, Payment.ReminderCooldownDays, "Tekrar hatırlatma bekleme süresi", "Ödeme ve Hatırlatma",
            "Aynı borç için yeniden hatırlatma gönderilmeden önce beklenecek gün sayısı.", "7",
            SystemSettingInputKind.Integer, true, 0, 365),
        new(11, Invitation.ValidityDays, "Davet geçerlilik süresi", "Davet",
            "Kullanıcı davet bağlantısının kaç gün geçerli kalacağı.", "7",
            SystemSettingInputKind.Integer, true, 1, 365),
        new(12, Invitation.ResendCooldownMinutes, "Davet tekrar gönderme bekleme süresi", "Davet",
            "Bir davetin yeniden gönderilebilmesi için beklenecek dakika.", "60",
            SystemSettingInputKind.Integer, true, 0, 10080),
        new(13, Lease.ExpiringSoonStatusDays, "Yakında bitecek sözleşme eşiği", "Sözleşme",
            "Birim ve sözleşme durumunda 'yakında bitiyor' kabul edilecek gün sayısı.", "30",
            SystemSettingInputKind.Integer, true, 0, 3650),
        new(14, Dashboard.ExpiringLeaseLookaheadDays, "Dashboard sözleşme görünüm süresi", "Dashboard",
            "Dashboard üzerinde yaklaşan sözleşmelerin kaç gün önceden gösterileceği.", "60",
            SystemSettingInputKind.Integer, true, 0, 3650),
        new(15, BankMatching.AmountTolerancePercent, "Tutar yakınlık toleransı", "Banka Eşleştirme",
            "Ödeme ve banka hareketi aday sıralamasında yakın tutar kabul edilecek yüzde.", "2",
            SystemSettingInputKind.Integer, true, 0, 100),
        new(16, BankMatching.DateToleranceDays, "Tarih yakınlık süresi", "Banka Eşleştirme",
            "Ödeme ve banka hareketi aday sıralamasında yakın tarih kabul edilecek gün sayısı.", "15",
            SystemSettingInputKind.Integer, true, 0, 365)
    ];

    private static readonly IReadOnlyDictionary<string, SystemSettingDefinition> ByKey =
        All.ToDictionary(definition => definition.Key, StringComparer.OrdinalIgnoreCase);

    public static SystemSettingDefinition? Find(string key)
        => ByKey.GetValueOrDefault(key);

    public static bool TryNormalizeValue(
        SystemSettingDefinition definition,
        string? value,
        out string normalizedValue,
        out string? error)
    {
        normalizedValue = value?.Trim() ?? string.Empty;
        error = null;

        if (definition.InputKind == SystemSettingInputKind.Integer)
        {
            if (!int.TryParse(normalizedValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
            {
                error = "Tam sayı bir değer girilmelidir.";
                return false;
            }

            if (definition.MinimumValue.HasValue && number < definition.MinimumValue.Value)
            {
                error = $"Değer en az {definition.MinimumValue.Value} olmalıdır.";
                return false;
            }

            if (definition.MaximumValue.HasValue && number > definition.MaximumValue.Value)
            {
                error = $"Değer en fazla {definition.MaximumValue.Value} olmalıdır.";
                return false;
            }

            normalizedValue = number.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        if (string.IsNullOrWhiteSpace(normalizedValue))
        {
            error = "Değer boş olamaz.";
            return false;
        }

        if (normalizedValue.Length > 2000)
        {
            error = "Değer en fazla 2000 karakter olabilir.";
            return false;
        }

        return true;
    }

    public static ReservationPolicySettings CreateReservationPolicy(
        IReadOnlyDictionary<string, string> values)
    {
        var normalizedValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var definition in All.Where(definition =>
                     definition.Key.StartsWith("Reservation.", StringComparison.OrdinalIgnoreCase)))
        {
            if (!values.TryGetValue(definition.Key, out var value))
                throw new InvalidOperationException($"Zorunlu sistem ayarı bulunamadı: {definition.Key}");

            if (!TryNormalizeValue(definition, value, out var normalizedValue, out var error))
                throw new InvalidOperationException($"{definition.Key}: {error}");

            normalizedValues[definition.Key] = normalizedValue;
        }

        var policy = new ReservationPolicySettings
        {
            MinimumDurationMinutes = ParseInt(normalizedValues, Reservation.MinimumDurationMinutes),
            MaximumDurationMinutes = ParseInt(normalizedValues, Reservation.MaximumDurationMinutes),
            MinimumAdvanceMinutes = ParseInt(normalizedValues, Reservation.MinimumAdvanceMinutes),
            MaximumAdvanceDays = ParseInt(normalizedValues, Reservation.MaximumAdvanceDays),
            ModificationCutoffMinutes = ParseInt(normalizedValues, Reservation.ModificationCutoffMinutes),
            CompletionGraceMinutes = ParseInt(normalizedValues, Reservation.CompletionGraceMinutes),
            MaximumAttendeeCount = ParseInt(normalizedValues, Reservation.MaximumAttendeeCount)
        };

        if (policy.MaximumDurationMinutes < policy.MinimumDurationMinutes)
            throw new InvalidOperationException(
                "Maksimum rezervasyon süresi minimum rezervasyon süresinden küçük olamaz.");

        return policy;
    }

    public static OperationalPolicySettings CreateOperationalPolicy(
        IReadOnlyDictionary<string, string> values)
        => new()
        {
            PaymentLinkValidityHours = ParseRequiredInt(values, Payment.LinkValidityHours),
            PaymentReminderDaysBefore = ParseRequiredInt(values, Payment.ReminderDaysBefore),
            PaymentReminderCooldownDays = ParseRequiredInt(values, Payment.ReminderCooldownDays),
            InvitationValidityDays = ParseRequiredInt(values, Invitation.ValidityDays),
            InvitationResendCooldownMinutes = ParseRequiredInt(values, Invitation.ResendCooldownMinutes),
            LeaseExpiringSoonStatusDays = ParseRequiredInt(values, Lease.ExpiringSoonStatusDays),
            DashboardExpiringLeaseLookaheadDays = ParseRequiredInt(values, Dashboard.ExpiringLeaseLookaheadDays),
            BankMatchingAmountTolerancePercent = ParseRequiredInt(values, BankMatching.AmountTolerancePercent),
            BankMatchingDateToleranceDays = ParseRequiredInt(values, BankMatching.DateToleranceDays)
        };

    private static int ParseRequiredInt(IReadOnlyDictionary<string, string> values, string key)
    {
        var definition = Find(key)
            ?? throw new InvalidOperationException($"Sistem ayarı tanımı bulunamadı: {key}");
        if (!values.TryGetValue(key, out var value))
            throw new InvalidOperationException($"Zorunlu sistem ayarı bulunamadı: {key}");
        if (!TryNormalizeValue(definition, value, out var normalizedValue, out var error))
            throw new InvalidOperationException($"{key}: {error}");

        return int.Parse(normalizedValue, NumberStyles.Integer, CultureInfo.InvariantCulture);
    }

    private static int ParseInt(IReadOnlyDictionary<string, string> values, string key)
        => int.Parse(values[key], NumberStyles.Integer, CultureInfo.InvariantCulture);
}
