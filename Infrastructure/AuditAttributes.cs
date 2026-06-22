namespace KiraTakip.Infrastructure;

[AttributeUsage(AttributeTargets.Property)]
public sealed class AuditIgnoreAttribute : Attribute { }

public enum MaskType { Email, Telefon, TcKimlik, VergiNo }

[AttributeUsage(AttributeTargets.Property)]
public sealed class AuditMaskAttribute : Attribute
{
    public MaskType MaskType { get; }
    public AuditMaskAttribute(MaskType maskType) => MaskType = maskType;
}
