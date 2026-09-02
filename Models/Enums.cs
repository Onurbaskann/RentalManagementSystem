namespace KiraTakip.Models;

public enum UserType
{
    Internal = 1,
    Tenant = 2
}

public enum RoleScope
{
    Internal = 1,
    Tenant = 2
}

public enum LeaseStatus
{
    Active = 1,
    Ended = 2,
    Terminated = 3,
    Draft = 4,
    RevisionRequested = 5
}

public enum LeaseReviewActionType
{
    DraftCreated = 1,
    DraftUpdated = 2,
    RevisionRequested = 3,
    Resubmitted = 4,
    Approved = 5,
    Deleted = 6
}

public enum LeaseActivityType
{
    Creation = 1,
    Extension = 2,
    Termination = 3,
    TufeIncrease = 4,
    KdvUpdate = 5,
    ChargeRegeneration = 6
}

public enum UnitStructure
{
    SingleUnit = 1,
    MultipleUnits = 2
}

public enum OccupancyStatus
{
    Vacant = 1,
    Leased = 2,
    ExpiringSoon = 3
}

public enum UnitTypeUsage
{
    Rentable = 1,
    Reservable = 2,
    NonRentable = 3
}

public enum ChargeStatus
{
    Pending = 1,
    PartiallyPaid = 2,
    Paid = 3,
    Overdue = 4,
    Cancelled = 5
}

public enum PaymentStatus
{
    PendingApproval = 1,
    Approved = 2,
    Rejected = 3
}

public enum PaymentChannel
{
    BankTransfer = 1,
    Eft = 2,
    Cash = 3,
    Other = 4
}

public enum BankMatchStatus
{
    Unmatched = 1,
    Matched = 2,
    ManuallyMatched = 3
}

public enum MatchType
{
    Automatic = 1,
    Manual = 2
}

public enum CalculationMethod
{
    Fixed = 1,
    M2 = 2
}

public enum LineItemSourceType
{
    UndefinedRate = 0,
    LeaseRateOverride = 1,
    UnitRateOverride = 2,
    RateSchedule = 3,
    PropertyRateOverride = 4,
    ManualInput = 5,
    ReservationRule = 6
}

public enum ChargeSourceType
{
    Lease = 1,
    Manual = 2,
    Reservation = 3
}

public enum ReservationStatus
{
    Confirmed = 1,
    Completed = 2,
    Cancelled = 3,
    PendingApproval = 5,
    Rejected = 6
}

public enum ChargeTypeBehavior
{
    MonthlyFixed = 1,
    FirstMonthOneTime = 2,
    UserManual = 3,
    ReservationSpecific = 4
}

public enum PaymentSourceType
{
    Manual = 1,
    BankMatch = 2,
    VirtualPos = 3
}

public enum DueDateRuleType
{
    FixedDayOfMonth = 1,
    PeriodStartOffset = 2
}

public enum InvitationStatus
{
    Pending = 1,
    Accepted = 2,
    Expired = 3,
    Cancelled = 4
}

public enum PasswordResetStatus
{
    Pending = 1,
    Used = 2,
    Expired = 3,
    Cancelled = 4
}

public enum DocumentOwnerType
{
    Tenant = 1,
    Payment = 2,
    Lease = 3,
    Template = 99
}

public enum OnlinePaymentTransactionStatus
{
    Pending = 1,
    Approved = 2,
    Failed = 3,
    Cancelled = 4,
    Unknown = 5
}

public enum OnlinePaymentEventType
{
    SessionRequested = 1,
    SessionResult = 2,
    CallbackReceived = 3,
    InquiryPerformed = 4,
    Succeeded = 5,
    Failed = 6
}
