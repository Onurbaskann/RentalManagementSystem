namespace KiraTakip.Models.Enums;

public enum SpaceUsage
{
    Rentable = 1,
    Reservable = 2,
    NotRentable = 3
}

public enum SpaceHistoryField
{
    Created = 1,
    Type = 2,
    Name = 3,
    Parent = 4,
    Area = 5,
    Usage = 6,
    PreAllocatable = 7,
    Shared = 8,
    Active = 9
}

public enum AttributeDataType
{
    Text = 1,
    Number = 2,
    Boolean = 3,
    List = 4
}
