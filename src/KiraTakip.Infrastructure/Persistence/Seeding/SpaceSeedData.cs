using KiraTakip.Data;
using KiraTakip.Domain.Spaces;
using KiraTakip.Models.Constants;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Infrastructure.Seeding;

public class SpaceSeedData(ApplicationDbContext ctx)
{
    private const string SeedUserId = "system";
    private const string City = "İzmir";
    private const string District = "Bornova";
    private const string Neighborhood = "Ege Üniversitesi";
    private const decimal Kdv = 20m;

    private const string Window = "PENCERE";
    private const string Capacity = "KAPASITE";
    private const string Floor = "ZEMIN";
    private const string Projector = "PROJEKTOR";

    private const string Campus = "KAMPUS";
    private const string Building = "BINA";
    private const string Storey = "KAT";
    private const string Office = "OFIS";
    private const string OpenOffice = "ACIK_OFIS";
    private const string Desk = "MASA";
    private const string MeetingRoom = "TOPLANTI";
    private const string EventArea = "ETKINLIK";
    private const string Storage = "DEPO";
    private const string Parking = "OTOPARK";
    private const string Technical = "KORIDOR_TEKNIK";

    private readonly List<Space> _created = [];
    private readonly Dictionary<string, SpaceType> _types = [];
    private readonly Dictionary<string, SpaceAttributeDefinition> _attributes = [];

    private sealed record TypeSeed(string Code, string Name, SpaceUsage Usage, bool PreAllocatable, params string[] Attributes);

    public async Task SeedAsync()
    {
        await SeedAttributesAsync();
        await SeedTypesAsync();

        if (await ctx.Spaces.AnyAsync()) return;

        await using var transaction = await ctx.Database.BeginTransactionAsync();
        await SeedTreeAsync();
        await FitAreasAsync();
        await SeedRatesAsync();
        await transaction.CommitAsync();
    }

    private async Task SeedAttributesAsync()
    {
        var seeds = new[]
        {
            (Code: Window, Name: "Pencere", Type: AttributeDataType.Boolean, Options: (string?)null),
            (Code: Capacity, Name: "Kapasite (kişi)", Type: AttributeDataType.Number, Options: null),
            (Code: Floor, Name: "Zemin", Type: AttributeDataType.List, Options: "Halı\nParke\nSeramik"),
            (Code: Projector, Name: "Projeksiyon", Type: AttributeDataType.Boolean, Options: null)
        };

        var existing = await ctx.SpaceAttributeDefinitions.ToListAsync();
        foreach (var seed in seeds)
        {
            if (existing.All(a => a.Code != seed.Code))
                ctx.SpaceAttributeDefinitions.Add(new SpaceAttributeDefinition { Code = seed.Code, Name = seed.Name, DataType = seed.Type, Options = seed.Options });
        }
        await ctx.SaveChangesAsync();

        foreach (var definition in await ctx.SpaceAttributeDefinitions.ToListAsync())
            _attributes[definition.Code] = definition;
    }

    private async Task SeedTypesAsync()
    {
        var seeds = new[]
        {
            new TypeSeed(Campus, "Kampüs", SpaceUsage.Rentable, false),
            new TypeSeed(Building, "Bina", SpaceUsage.Rentable, false),
            new TypeSeed(Storey, "Kat", SpaceUsage.Rentable, false),
            new TypeSeed(Office, "Ofis", SpaceUsage.Rentable, true, Window, Capacity, Floor),
            new TypeSeed(OpenOffice, "Açık Ofis", SpaceUsage.Rentable, false, Capacity, Floor),
            new TypeSeed(Desk, "Masa", SpaceUsage.Rentable, true),
            new TypeSeed(MeetingRoom, "Toplantı Salonu", SpaceUsage.Reservable, false, Capacity, Projector),
            new TypeSeed(EventArea, "Etkinlik Alanı", SpaceUsage.Reservable, false, Capacity, Projector),
            new TypeSeed(Storage, "Depo", SpaceUsage.Rentable, false),
            new TypeSeed(Parking, "Otopark Alanı", SpaceUsage.Rentable, false),
            new TypeSeed(Technical, "Koridor/Teknik", SpaceUsage.NotRentable, false)
        };

        var existing = await ctx.SpaceTypes.Include(t => t.DefaultAttributes).ToListAsync();
        var order = 1;
        foreach (var seed in seeds)
        {
            var type = existing.FirstOrDefault(t => t.Code == seed.Code);
            if (type is null)
            {
                type = new SpaceType
                {
                    Code = seed.Code,
                    Name = seed.Name,
                    SortOrder = order,
                    DefaultUsage = seed.Usage,
                    DefaultIsPreAllocatable = seed.PreAllocatable
                };
                ctx.SpaceTypes.Add(type);
                existing.Add(type);

                foreach (var attributeCode in seed.Attributes)
                    type.DefaultAttributes.Add(new SpaceTypeDefaultAttribute { AttributeDefinition = _attributes[attributeCode] });
            }
            order++;
        }
        await ctx.SaveChangesAsync();

        foreach (var type in existing)
            _types[type.Code] = type;
    }

    private async Task SeedTreeAsync()
    {
        var a = await AddAsync(null, Building, "A", "Teknokent A Blok", 5000, address: Address("Ege Üniversitesi Teknokent Kampüsü"));
        var aZemin = await AddAsync(a, Storey, "ZEMIN", "A Blok Zemin Kat", 1500);
        await AddAsync(aZemin, MeetingRoom, "Z01", "Toplantı Salonu Z01", 60, values: [(Capacity, "20"), (Projector, "True")]);
        await AddAsync(aZemin, MeetingRoom, "Z02", "Toplantı Odası Z02", 30, values: [(Capacity, "8"), (Projector, "False")]);
        await AddAsync(aZemin, EventArea, "KONF", "A Blok Konferans Salonu", 250, values: [(Capacity, "200"), (Projector, "True")]);
        await AddAsync(aZemin, EventArea, "FUAYE", "A Blok Fuaye ve Sergi Alanı", 120, values: [(Capacity, "120")]);
        await AddAsync(aZemin, Technical, "TEKNIK", "A Blok Teknik Oda", 25);
        await AddAsync(aZemin, Technical, "KORIDOR", "A Blok Zemin Koridor", 80);
        var aKat1 = await AddAsync(a, Storey, "K1", "A Blok 1. Kat", 1200);
        await AddAsync(aKat1, Office, "101", "Ofis A-101", 45, values: [(Window, "True"), (Capacity, "4"), (Floor, "Halı")]);
        await AddAsync(aKat1, Office, "102", "Ofis A-102", 60, values: [(Window, "True"), (Capacity, "6"), (Floor, "Parke")]);
        await AddAsync(aKat1, Office, "103", "Ofis A-103", 52, preAllocatable: false);
        await AddAsync(aKat1, Office, "104", "Ofis A-104 (Paylaşımlı)", 90, shared: true);
        var aKat2 = await AddAsync(a, Storey, "K2", "A Blok 2. Kat", 1000);
        var ortak = await AddAsync(aKat2, OpenOffice, "ORTAK", "Ortak Çalışma Alanı", 200, values: [(Capacity, "20"), (Floor, "Halı")]);
        for (var i = 1; i <= 6; i++)
            await AddAsync(ortak, Desk, $"M{i:00}", $"Masa A-M{i:00}", 4);

        var b = await AddAsync(null, Building, "B", "Teknokent B Blok", 3500, address: Address("Ege Üniversitesi Teknokent Kampüsü B Blok"));
        var bZemin = await AddAsync(b, Storey, "ZEMIN", "B Blok Zemin Kat", 1200);
        await AddAsync(bZemin, MeetingRoom, "TS1", "B Blok Toplantı Salonu 1", 50, values: [(Capacity, "16"), (Projector, "True")]);
        await AddAsync(bZemin, MeetingRoom, "TS2", "B Blok Toplantı Salonu 2", 40, values: [(Capacity, "12"), (Projector, "True")]);
        await AddAsync(bZemin, EventArea, "KONF", "B Blok Konferans ve Etkinlik Salonu", 300, values: [(Capacity, "250"), (Projector, "True")]);
        var bKat1 = await AddAsync(b, Storey, "K1", "B Blok 1. Kat", 1000);
        await AddAsync(bKat1, Office, "101", "Ofis B-101", 55, values: [(Window, "True"), (Capacity, "5")]);
        await AddAsync(bKat1, Office, "102", "Ofis B-102", 70, values: [(Window, "False"), (Capacity, "8")]);

        var otopark = await AddAsync(null, Parking, "OTOPARK", "Teknokent Açık Otoparkı", 3000, address: Address("Ege Üniversitesi Teknokent Kampüsü Açık Otopark Alanı"));
        for (var i = 1; i <= 4; i++)
            await AddAsync(otopark, Parking, $"P{i:00}", $"Otopark Yeri P-{i:00}", 12);

        var c = await AddAsync(null, Building, "C", "Teknokent C Blok (Ar-Ge Merkezi)", 3000, address: Address("Ege Üniversitesi Teknokent Kampüsü C Blok"));
        var cBodrum = await AddAsync(c, Storey, "BODRUM", "C Blok Bodrum Kat", 600);
        await AddAsync(cBodrum, Storage, "DEPO1", "C Blok Depo 1", 80);
        await AddAsync(cBodrum, Technical, "TEKNIK", "C Blok Elektrik Odası", 30);
        var cKat1 = await AddAsync(c, Storey, "K1", "C Blok 1. Kat", 900);
        await AddAsync(cKat1, Office, "101", "Ofis C-101 (Ar-Ge Ofisi)", 85);
        await AddAsync(cKat1, Office, "102", "Ofis C-102 (Yazılım Laboratuvarı)", 110);
        var cKat2 = await AddAsync(c, Storey, "K2", "C Blok 2. Kat", 900);
        await AddAsync(cKat2, Office, "201", "Ofis C-201 (Açık İnovasyon Ofisi)", 140);
        await AddAsync(cKat2, MeetingRoom, "TS", "C Blok Toplantı Salonu", 45, values: [(Capacity, "14"), (Projector, "True")]);

        var d = await AddAsync(null, Building, "D", "Teknokent D Blok (Kuluçka ve Girişimcilik Merkezi)", 2500, address: Address("Ege Üniversitesi Teknokent Kampüsü D Blok"));
        var kulucka = await AddAsync(d, OpenOffice, "KULUCKA", "Kuluçka Merkezi", 150, values: [(Capacity, "16")]);
        for (var i = 1; i <= 4; i++)
            await AddAsync(kulucka, Desk, $"K{i:00}", $"Kuluçka Masası K-{i:00}", 4);
        await AddAsync(d, MeetingRoom, "SEMINER", "D Blok Seminer ve Toplantı Odası", 55, values: [(Capacity, "30"), (Projector, "True")]);
        var dKat2 = await AddAsync(d, Storey, "K2", "D Blok 2. Kat", 700);
        await AddAsync(dKat2, Office, "201", "Girişim Ofisi D-201", 50);
        await AddAsync(dKat2, Office, "202", "Girişim Ofisi D-202 (Paylaşımlı)", 75, shared: true);

        var kampus = await AddAsync(null, Campus, "ETAP2", "Teknokent 2. Etap Kampüsü", 12000, address: Address("Ege Üniversitesi Teknokent 2. Etap Yerleşkesi"));
        var eBlok = await AddAsync(kampus, Building, "E", "E Blok", 2000);
        var eKat1 = await AddAsync(eBlok, Storey, "K1", "E Blok 1. Kat", 800);
        await AddAsync(eKat1, Office, "101", "Ofis E-101", 65, values: [(Window, "True"), (Capacity, "6")]);
        await AddAsync(eKat1, Office, "102", "Ofis E-102", 80, values: [(Window, "True"), (Capacity, "8")]);
        await AddAsync(kampus, EventArea, "ACIKHAVA", "Açık Hava Etkinlik Alanı", 600, values: [(Capacity, "400")]);
    }

    private static SpaceAddress Address(string openAddress) => new()
    {
        City = City,
        District = District,
        Neighborhood = Neighborhood,
        Address = openAddress
    };

    private async Task<Space> AddAsync(
        Space? parent,
        string typeCode,
        string code,
        string name,
        decimal area,
        bool? preAllocatable = null,
        bool shared = false,
        SpaceAddress? address = null,
        (string Attribute, string Value)[]? values = null)
    {
        var type = _types[typeCode];
        var usage = type.DefaultUsage;
        var isShared = shared && usage == SpaceUsage.Rentable;
        var allocatable = usage == SpaceUsage.Rentable && !isShared && (preAllocatable ?? type.DefaultIsPreAllocatable);
        var depth = parent is null ? 0 : parent.Depth + 1;
        if (depth > SpaceTreePolicy.MaxDepth)
            throw new InvalidOperationException($"Alan ağacı derinliği {SpaceTreePolicy.MaxDepth} seviyeyi aşamaz.");

        var space = new Space
        {
            ParentId = parent?.Id,
            SpaceTypeId = type.Id,
            Code = code,
            Name = name,
            SortOrder = _created.Count(s => s.ParentId == parent?.Id) + 1,
            Path = "/",
            Depth = depth,
            Area = area,
            Usage = usage,
            IsPreAllocatable = allocatable,
            IsShared = isShared
        };
        ctx.Spaces.Add(space);
        await ctx.SaveChangesAsync();

        space.Path = SpaceTreePolicy.BuildPath(parent?.Path, space.Id);

        if (address is not null)
        {
            address.SpaceId = space.Id;
            ctx.SpaceAddresses.Add(address);
        }

        foreach (var (attribute, value) in values ?? [])
            ctx.SpaceAttributeValues.Add(new SpaceAttributeValue { SpaceId = space.Id, AttributeDefinitionId = _attributes[attribute].Id, Value = value });

        ctx.SpaceHistories.Add(new SpaceHistory
        {
            SpaceId = space.Id,
            Field = SpaceHistoryField.Created,
            NewValue = space.Name,
            ChangedAt = DateTime.UtcNow,
            UserId = SeedUserId
        });
        await ctx.SaveChangesAsync();

        _created.Add(space);
        return space;
    }

    private async Task FitAreasAsync()
    {
        foreach (var space in _created.OrderByDescending(s => s.Depth))
        {
            var childTotal = _created.Where(c => c.ParentId == space.Id).Sum(c => c.Area);
            if (childTotal > space.Area)
                space.Area = childTotal;
        }
        await ctx.SaveChangesAsync();
    }

    private async Task SeedRatesAsync()
    {
        var categories = await ctx.Kategoriler
            .Where(k => k.Type == CategoryType.Tenant)
            .ToDictionaryAsync(k => k.Code, k => k.Id);
        var chargeTypes = await ctx.ChargeTypes.ToDictionaryAsync(b => b.Code, b => b.Id);

        if (!categories.TryGetValue("AKADEMIK", out var academic) ||
            !categories.TryGetValue("AKADEMIK_OLMAYAN", out var commercial) ||
            !chargeTypes.TryGetValue(BorcTipiConsts.Kira, out var rent) ||
            !chargeTypes.TryGetValue("ORTAK", out var common))
            return;

        Space Find(string name) => _created.First(s => s.Name == name);

        void AddRates(Space space, int chargeTypeId, CalculationMethod method, decimal academicValue, decimal commercialValue)
        {
            ctx.SpaceRates.Add(new SpaceRate { SpaceId = space.Id, TenantCategoryId = academic, ChargeTypeId = chargeTypeId, CalculationMethod = method, UnitValue = academicValue, KdvRate = Kdv });
            ctx.SpaceRates.Add(new SpaceRate { SpaceId = space.Id, TenantCategoryId = commercial, ChargeTypeId = chargeTypeId, CalculationMethod = method, UnitValue = commercialValue, KdvRate = Kdv });
        }

        foreach (var root in _created.Where(s => s.ParentId is null && s.Usage == SpaceUsage.Rentable))
        {
            AddRates(root, rent, CalculationMethod.M2, 300, 450);
            AddRates(root, common, CalculationMethod.M2, 100, 150);
        }

        AddRates(Find("A Blok 1. Kat"), rent, CalculationMethod.M2, 320, 480);
        AddRates(Find("B Blok 1. Kat"), rent, CalculationMethod.M2, 310, 470);
        AddRates(Find("C Blok 1. Kat"), rent, CalculationMethod.M2, 340, 500);
        AddRates(Find("Ortak Çalışma Alanı"), rent, CalculationMethod.Fixed, 2500, 3500);
        AddRates(Find("Kuluçka Merkezi"), rent, CalculationMethod.Fixed, 1800, 3000);
        AddRates(Find("E Blok"), common, CalculationMethod.M2, 110, 165);

        await ctx.SaveChangesAsync();
    }
}
