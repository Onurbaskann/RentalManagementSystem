namespace KiraTakip.Infrastructure;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class SuppressAutomaticSuccessFeedbackAttribute : Attribute;
