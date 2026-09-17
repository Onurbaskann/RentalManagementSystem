using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Domain.Reservations;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Common;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Dtos.Reservation;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Enums;
using KiraTakip.Web.Controllers;
using KiraTakip.Web.Models.ViewModels;
using KiraTakip.Repositories.Charges;
using KiraTakip.Repositories.Properties;
using KiraTakip.Repositories.Reservations;
using KiraTakip.Repositories.Tenants;
using KiraTakip.Services.Reservations;
using KiraTakip.Web.Validators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Reflection;

namespace KiraTakip.Tests;

public class ReservationValidationTests
{
    [Fact]
    public void CreateValidator_ShouldRejectInvalidRequiredValuesAndDateOrder()
    {
        var result = new ReservationCreateViewModelValidator().Validate(new ReservationCreateViewModel
        {
            StartDate = new DateTime(2026, 2, 2),
            EndDate = new DateTime(2026, 2, 1),
            Description = new string('a', 501)
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Field == nameof(ReservationCreateViewModel.UnitId));
        Assert.Contains(result.Errors, error => error.Field == nameof(ReservationCreateViewModel.TenantId));
        Assert.Contains(result.Errors, error => error.Field == nameof(ReservationCreateViewModel.EndDate));
        Assert.Contains(result.Errors, error => error.Field == nameof(ReservationCreateViewModel.Description));
    }

    [Fact]
    public void TenantCreateValidator_ShouldRejectDuplicateAndInvalidAttendeeEmails()
    {
        var result = new TenantReservationCreateViewModelValidator().Validate(
            new TenantReservationCreateViewModel
            {
                UnitId = 1,
                Title = "Toplantı",
                StartDate = new DateTime(2026, 9, 1, 9, 0, 0),
                EndDate = new DateTime(2026, 9, 1, 10, 0, 0),
                Attendees =
                [
                    new() { DisplayName = "Bir", EmailAddress = "katilimci@example.com" },
                    new() { DisplayName = "İki", EmailAddress = "KATILIMCI@example.com" },
                    new() { DisplayName = "Üç", EmailAddress = "geçersiz" }
                ]
            });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Field == "Attendees[1].EmailAddress");
        Assert.Contains(result.Errors, error => error.Field == "Attendees[2].EmailAddress");
    }

    [Fact]
    public void TenantCreateModel_ShouldNotAcceptTenantOrApprovalFromForm()
    {
        Assert.Null(typeof(TenantReservationCreateViewModel).GetProperty("TenantId"));
        Assert.Null(typeof(TenantReservationCreateViewModel).GetProperty("CreateAndApprove"));
        Assert.Null(typeof(TenantReservationCreateViewModel).GetProperty("InternalNotes"));
    }

    [Fact]
    public void CancelValidator_ShouldRejectBlankOrOversizedReason()
    {
        var validator = new CancelReservationViewModelValidator();

        Assert.False(validator.Validate(new CancelReservationViewModel()).IsValid);
        Assert.False(validator.Validate(new CancelReservationViewModel
        {
            Reason = new string('a', 451)
        }).IsValid);
    }

    [Fact]
    public void CalculationValidator_ShouldRejectInvalidDateTextAndOrder()
    {
        var validator = new ReservationCalculationQueryViewModelValidator();
        var invalidText = validator.Validate(new ReservationCalculationQueryViewModel
        {
            UnitId = 1,
            Start = "geçersiz",
            End = "2026-01-01T10:00"
        });
        var invalidOrder = validator.Validate(new ReservationCalculationQueryViewModel
        {
            UnitId = 1,
            Start = "2026-01-01T11:00",
            End = "2026-01-01T10:00"
        });

        Assert.False(invalidText.IsValid);
        Assert.Contains(invalidText.Errors, error => error.Field == nameof(ReservationCalculationQueryViewModel.Start));
        Assert.False(invalidOrder.IsValid);
        Assert.Contains(invalidOrder.Errors, error => error.Field == nameof(ReservationCalculationQueryViewModel.End));
    }

    [Fact]
    public void CalculateAction_ShouldRequireReservationModulePermission()
    {
        var method = typeof(ReservationController)
            .GetMethod(nameof(ReservationController.Calculate));

        var policy = Assert.Single(method!.GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal(KiraTakip.Authorization.PermissionCatalog.Reservation.Module, policy.Policy);
    }

    [Theory]
    [InlineData(nameof(ReservationController.Approve), PermissionCatalog.Reservation.Approve)]
    [InlineData(nameof(ReservationController.Reject), PermissionCatalog.Reservation.Reject)]
    public void DecisionActions_ShouldRequireExplicitPermission(string actionName, string permission)
    {
        var methods = typeof(ReservationController)
            .GetMethods()
            .Where(method => method.Name == actionName);
        Assert.All(methods, method => Assert.Contains(
            method.GetCustomAttributes<AuthorizeAttribute>(),
            attribute => attribute.Policy == permission));
    }

    [Fact]
    public void EditActions_ShouldRequireModuleForGetAndEditForPost()
    {
        var methods = typeof(ReservationController)
            .GetMethods()
            .Where(method => method.Name == nameof(ReservationController.Edit))
            .ToList();
        Assert.Equal(2, methods.Count);
        var getMethod = Assert.Single(methods, m => m.GetCustomAttributes<Microsoft.AspNetCore.Mvc.HttpGetAttribute>().Any());
        var postMethod = Assert.Single(methods, m => m.GetCustomAttributes<Microsoft.AspNetCore.Mvc.HttpPostAttribute>().Any());
        Assert.Contains(getMethod.GetCustomAttributes<AuthorizeAttribute>(), a => a.Policy == PermissionCatalog.Reservation.Module);
        Assert.Contains(postMethod.GetCustomAttributes<AuthorizeAttribute>(), a => a.Policy == PermissionCatalog.Reservation.Edit);
    }

    [Theory]
    [InlineData(nameof(ReservationController.Details), "Details/{id}")]
    [InlineData(nameof(ReservationController.Cancel), "Cancel/{id}")]
    [InlineData(nameof(ReservationController.TransferToCharge), "TransferToCharge/{id}")]
    public void HashedIdActions_ShouldNotUseIntegerRouteConstraint(
        string actionName,
        string expectedTemplate)
    {
        var method = typeof(ReservationController)
            .GetMethod(actionName);
        var routeTemplate = method!.GetCustomAttributes<HttpMethodAttribute>()
            .Single()
            .Template;

        Assert.Equal(expectedTemplate, routeTemplate);
    }

    [Fact]
    public void DetailViews_ShouldShowRejectedReservationReasonSafely()
    {
        var repoRoot = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", ".."));
        var viewsRoot = Directory.Exists(Path.Combine(repoRoot, "src", "KiraTakip.Web", "Views"))
            ? Path.Combine(repoRoot, "src", "KiraTakip.Web", "Views")
            : Path.Combine(repoRoot, "Views");
        var internalDetails = File.ReadAllText(Path.Combine(
            viewsRoot, "Reservation", "Details.cshtml"));
        var tenantDetails = File.ReadAllText(Path.Combine(
            viewsRoot, "TenantReservation", "Details.cshtml"));

        Assert.Contains("Model.RejectionReason", internalDetails);
        Assert.Contains("Model.RejectedByDisplayName", internalDetails);
        Assert.Contains("Model.RejectedAt", internalDetails);
        Assert.Contains("Model.RejectionReason", tenantDetails);
        Assert.Contains("Model.RejectedAt", tenantDetails);
        Assert.DoesNotContain("Model.RejectedByDisplayName", tenantDetails);
    }
}

[Collection("Database collection")]
public class ReservationArchitectureTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IDbContextTransaction _transaction;

    public ReservationArchitectureTests(DatabaseFixture fixture)
    {
        _context = fixture.CreateContext();
        _transaction = _context.Database.BeginTransaction();
    }

    public void Dispose()
    {
        _transaction.Rollback();
        _transaction.Dispose();
        _context.Dispose();
    }

    [Theory]
    [InlineData(ReservationStatus.PendingApproval, ReservationStatus.Confirmed, true)]
    [InlineData(ReservationStatus.PendingApproval, ReservationStatus.Rejected, true)]
    [InlineData(ReservationStatus.Confirmed, ReservationStatus.Completed, true)]
    [InlineData(ReservationStatus.Completed, ReservationStatus.Confirmed, false)]
    [InlineData(ReservationStatus.Cancelled, ReservationStatus.Confirmed, false)]
    public void Lifecycle_ShouldAllowOnlyDefinedTransitions(
        ReservationStatus from,
        ReservationStatus to,
        bool expected)
    {
        Assert.Equal(expected, ReservationLifecycle.CanTransition(from, to));
    }

    [Fact]
    public void Model_ShouldConfigureReservationConcurrencyAndAttendeeUniqueness()
    {
        var reservationType = _context.Model.FindEntityType(typeof(Reservation));
        var attendeeType = _context.Model.FindEntityType(typeof(ReservationAttendee));

        Assert.NotNull(reservationType);
        Assert.True(reservationType!.FindProperty(nameof(Reservation.RowVersion))!.IsConcurrencyToken);
        Assert.NotNull(attendeeType);
        Assert.Contains(attendeeType!.GetIndexes(), index =>
            index.IsUnique
            && index.Properties.Select(property => property.Name).SequenceEqual(
                [nameof(ReservationAttendee.ReservationId), nameof(ReservationAttendee.NormalizedEmailAddress)]));
    }

    [Fact]
    public async Task List_ShouldCombinePropertyAndDirectUnitScopes()
    {
        var seed = await SeedAsync();
        await AddReservationAsync(seed.FirstUnitId, seed.ActiveTenantId);
        await AddReservationAsync(seed.SecondUnitId, seed.ActiveTenantId);
        await AddReservationAsync(seed.OutsideUnitId, seed.ActiveTenantId);

        var result = await CreateService().GetAllAsync(
            new GetReservationsInput([seed.FirstPropertyId], [seed.SecondUnitId]));

        Assert.Equal(2, result.Count);
        Assert.Contains(result, reservation => reservation.UnitId == seed.FirstUnitId);
        Assert.Contains(result, reservation => reservation.UnitId == seed.SecondUnitId);
        Assert.DoesNotContain(result, reservation => reservation.UnitId == seed.OutsideUnitId);
    }

    [Fact]
    public async Task Details_ShouldAcceptDirectUnitScopeAndRejectOutsideScope()
    {
        var seed = await SeedAsync();
        var reservation = await AddReservationAsync(seed.SecondUnitId, seed.ActiveTenantId);
        var service = CreateService();

        var result = await service.GetByIdAsync(new GetReservationByIdInput(
            reservation.Id,
            new ReservationAccessScopeInput([], [seed.SecondUnitId])));

        Assert.Equal(reservation.Id, result.Id);
        var exception = await Assert.ThrowsAsync<BusinessException>(() => service.GetByIdAsync(
            new GetReservationByIdInput(
                reservation.Id,
                new ReservationAccessScopeInput([], []))));
        Assert.Equal("RESERVATION_OUT_OF_SCOPE", exception.Code);
    }

    [Fact]
    public async Task FormOptions_ShouldFilterUnitsByScopeAndExcludeInactiveTenants()
    {
        var seed = await SeedAsync();

        var options = await CreateService().GetFormOptionsAsync(
            new GetReservationFormOptionsInput(
                new ReservationAccessScopeInput([], [seed.SecondUnitId])));

        var unit = Assert.Single(options.Units);
        Assert.Equal(seed.SecondUnitId, unit.Id);
        Assert.Contains(options.Tenants, tenant => tenant.Id == seed.ActiveTenantId);
        Assert.DoesNotContain(options.Tenants, tenant => tenant.Id == seed.InactiveTenantId);
    }

    [Fact]
    public async Task Create_ShouldRejectOutsideScopeBeforeMutation()
    {
        var seed = await SeedAsync();
        var service = CreateService();
        var reservationCount = await _context.Reservations.CountAsync();

        var exception = await Assert.ThrowsAsync<BusinessException>(() => service.CreateAsync(
            BuildCreateInput(
                seed.FirstUnitId,
                seed.ActiveTenantId,
                new ReservationAccessScopeInput([], [seed.SecondUnitId]))));

        Assert.Equal("RESERVATION_OUT_OF_SCOPE", exception.Code);
        Assert.Empty(_context.Reservations.Local);
        Assert.Equal(reservationCount, await _context.Reservations.CountAsync());
    }

    [Fact]
    public async Task Create_ShouldRejectMissingRateAndOverlappingReservation()
    {
        var seed = await SeedAsync();
        var service = CreateService();
        var directNoRateScope = new ReservationAccessScopeInput([], [seed.NoRateUnitId]);

        var missingRate = await Assert.ThrowsAsync<BusinessValidationException>(() => service.CreateAsync(
            BuildCreateInput(seed.NoRateUnitId, seed.ActiveTenantId, directNoRateScope)));
        Assert.Equal("RESERVATION_RATE_NOT_FOUND", missingRate.Code);

        await AddReservationAsync(seed.FirstUnitId, seed.ActiveTenantId);
        var overlap = await Assert.ThrowsAsync<BusinessValidationException>(() => service.CreateAsync(
            BuildCreateInput(
                seed.FirstUnitId,
                seed.ActiveTenantId,
                new ReservationAccessScopeInput([seed.FirstPropertyId], []))));
        Assert.Equal("RESERVATION_TIME_CONFLICT", overlap.Code);
    }

    [Fact]
    public async Task Create_ShouldRejectInactiveTenant()
    {
        var seed = await SeedAsync();

        var exception = await Assert.ThrowsAsync<BusinessValidationException>(() => CreateService().CreateAsync(
            BuildCreateInput(
                seed.FirstUnitId,
                seed.InactiveTenantId,
                new ReservationAccessScopeInput([seed.FirstPropertyId], []))));

        Assert.Equal("RESERVATION_TENANT_NOT_ACTIVE", exception.Code);
    }

    [Fact]
    public async Task CreateRequest_ShouldAllowPendingRequestsInSameSlotAndSaveSnapshotAttendees()
    {
        var seed = await SeedAsync();
        var user = await _context.Users.FirstAsync();
        var service = CreateService();
        var scope = new ReservationAccessScopeInput([seed.FirstPropertyId], []);
        var firstId = await service.CreateRequestAsync(BuildRequestInput(
            seed.FirstUnitId,
            seed.ActiveTenantId,
            user.Id,
            user.Email!,
            scope));
        var secondId = await service.CreateRequestAsync(BuildRequestInput(
            seed.FirstUnitId,
            seed.ActiveTenantId,
            user.Id,
            user.Email!,
            scope));

        var reservations = await _context.Reservations
            .Where(reservation => reservation.Id == firstId || reservation.Id == secondId)
            .Include(reservation => reservation.Attendees)
            .OrderBy(reservation => reservation.Id)
            .ToListAsync();
        Assert.Equal(2, reservations.Count);
        Assert.All(reservations, reservation => Assert.Equal(ReservationStatus.PendingApproval, reservation.Status));
        Assert.All(reservations, reservation => Assert.Equal(user.Id, reservation.RequestedByUserId));
        Assert.All(reservations, reservation => Assert.False(string.IsNullOrWhiteSpace(reservation.CreatedBy)));
        Assert.All(reservations, reservation => Assert.Equal(2, reservation.Attendees.Count));
        Assert.All(reservations, reservation => Assert.Single(
            reservation.Attendees,
            attendee => attendee.IsReservationOwner));
    }

    [Fact]
    public async Task CreateAndApprove_ShouldRecheckConfirmedConflict()
    {
        var seed = await SeedAsync();
        var user = await _context.Users.FirstAsync();
        var service = CreateService();
        var scope = new ReservationAccessScopeInput([seed.FirstPropertyId], []);
        var input = BuildRequestInput(
            seed.FirstUnitId,
            seed.ActiveTenantId,
            user.Id,
            user.Email!,
            scope,
            createAndApprove: true);

        var id = await service.CreateRequestAsync(input);
        Assert.Equal(
            ReservationStatus.Confirmed,
            await _context.Reservations.Where(reservation => reservation.Id == id).Select(reservation => reservation.Status).SingleAsync());

        var exception = await Assert.ThrowsAsync<BusinessValidationException>(() =>
            service.CreateRequestAsync(input));
        Assert.Equal("RESERVATION_TIME_CONFLICT", exception.Code);
    }

    [Fact]
    public async Task TenantDetailsAndCancel_ShouldEnforceOwnership()
    {
        var seed = await SeedAsync();
        var reservation = await AddReservationAsync(
            seed.FirstUnitId,
            seed.ActiveTenantId,
            ReservationStatus.PendingApproval);
        var service = CreateService();
        var scope = new ReservationAccessScopeInput([seed.FirstPropertyId], []);
        var actor = await _context.Users.FirstAsync();

        var forbidden = await Assert.ThrowsAsync<BusinessException>(() =>
            service.GetTenantByIdAsync(new GetTenantReservationByIdInput(
                reservation.Id,
                seed.InactiveTenantId,
                scope)));
        Assert.Equal("RESERVATION_TENANT_SCOPE_VIOLATION", forbidden.Code);

        await service.CancelTenantAsync(new CancelTenantReservationInput(
            reservation.Id,
            seed.ActiveTenantId,
            "Artık ihtiyaç bulunmuyor.",
            scope,
            actor.Id));

        Assert.Equal(ReservationStatus.Cancelled, reservation.Status);
        Assert.Equal("Artık ihtiyaç bulunmuyor.", reservation.CancellationReason);
        Assert.Equal(actor.Id, reservation.CancelledByUserId);
    }

    [Fact]
    public async Task ApproveAndReject_ShouldEnforceConflictRowVersionAndAuditFields()
    {
        var seed = await SeedAsync();
        var actor = await _context.Users.FirstAsync();
        var first = await AddReservationAsync(seed.FirstUnitId, seed.ActiveTenantId, ReservationStatus.PendingApproval);
        var second = await AddReservationAsync(seed.FirstUnitId, seed.ActiveTenantId, ReservationStatus.PendingApproval);
        var firstVersion = first.RowVersion.ToArray();
        var secondVersion = second.RowVersion.ToArray();
        var scope = new ReservationAccessScopeInput([seed.FirstPropertyId], []);
        var service = CreateService();

        await service.ApproveAsync(new ApproveReservationInput(
            first.Id, firstVersion, actor.Id, scope));
        Assert.Equal(ReservationStatus.Confirmed, first.Status);
        Assert.Equal(actor.Id, first.ApprovedByUserId);
        Assert.NotNull(first.ApprovedAt);

        var conflict = await Assert.ThrowsAsync<BusinessException>(() =>
            service.ApproveAsync(new ApproveReservationInput(
                second.Id, secondVersion, actor.Id, scope)));
        Assert.Equal("RESERVATION_APPROVAL_TIME_CONFLICT", conflict.Code);

        var stale = await Assert.ThrowsAsync<BusinessException>(() =>
            service.RejectAsync(new RejectReservationInput(
                first.Id, "Uygun değil.", firstVersion, actor.Id, scope)));
        Assert.Equal("RESERVATION_STALE_VERSION", stale.Code);

        await service.RejectAsync(new RejectReservationInput(
            second.Id, "Başka bir rezervasyon onaylandı.", second.RowVersion, actor.Id, scope));
        Assert.Equal(ReservationStatus.Rejected, second.Status);
        Assert.Equal(actor.Id, second.RejectedByUserId);
        Assert.NotNull(second.RejectedAt);
    }

    [Fact]
    public async Task Update_ShouldRecalculatePriceReplaceAttendeesAndRejectTerminalStatus()
    {
        var seed = await SeedAsync();
        var actor = await _context.Users.FirstAsync();
        var reservation = await AddReservationAsync(seed.FirstUnitId, seed.ActiveTenantId, ReservationStatus.PendingApproval);
        reservation.RequestedByDisplayNameSnapshot = "Talep Sahibi";
        reservation.RequestedByEmailSnapshot = actor.Email;
        reservation.Attendees.Add(new ReservationAttendee
        {
            DisplayName = "Talep Sahibi",
            EmailAddress = actor.Email!,
            NormalizedEmailAddress = actor.Email!.ToUpperInvariant(),
            IsReservationOwner = true
        });
        await _context.SaveChangesAsync();
        var service = CreateService();
        var scope = new ReservationAccessScopeInput([seed.FirstPropertyId], []);

        await service.UpdateAsync(new UpdateReservationInput(
            reservation.Id,
            seed.FirstUnitId,
            seed.ActiveTenantId,
            new DateTime(2026, 1, 1, 9, 0, 0),
            new DateTime(2026, 1, 1, 10, 0, 0),
            "Güncel başlık",
            "Güncel açıklama",
            "Güncel not",
            "İç not",
            [new ReservationAttendeePolicyInput("Yeni Katılımcı", "new@example.com", false)],
            reservation.RowVersion,
            actor.Id,
            actor.AdSoyad ?? actor.Email!,
            actor.Email!,
            false,
            null,
            scope));

        Assert.Equal("Güncel başlık", reservation.Title);
        Assert.Equal(100m, reservation.RateAmount);
        Assert.Equal(2, reservation.Attendees.Count);
        Assert.Contains(reservation.Attendees, attendee => attendee.EmailAddress == "new@example.com");

        reservation.Status = ReservationStatus.Rejected;
        await _context.SaveChangesAsync();
        var terminal = await Assert.ThrowsAsync<BusinessException>(() =>
            service.UpdateAsync(new UpdateReservationInput(
                reservation.Id,
                seed.FirstUnitId,
                seed.ActiveTenantId,
                reservation.StartDate,
                reservation.EndDate,
                reservation.Title!,
                reservation.Description,
                reservation.Notes,
                reservation.InternalNotes,
                [],
                reservation.RowVersion,
                actor.Id,
                actor.AdSoyad ?? actor.Email!,
                actor.Email!,
                false,
                null,
                scope)));
        Assert.Equal("RESERVATION_CANNOT_BE_MODIFIED", terminal.Code);
    }

    [Fact]
    public async Task Calculate_ShouldRejectOutsideScope()
    {
        var seed = await SeedAsync();

        var exception = await Assert.ThrowsAsync<BusinessException>(() => CreateService().CalculateAsync(
            new CalculateReservationInput(
                seed.FirstUnitId,
                new DateTime(2026, 1, 1, 9, 0, 0),
                new DateTime(2026, 1, 1, 11, 0, 0),
                new ReservationAccessScopeInput([], [seed.SecondUnitId]))));

        Assert.Equal("RESERVATION_OUT_OF_SCOPE", exception.Code);
    }

    [Fact]
    public async Task Cancel_ShouldRejectCompletedReservationWithoutMutation()
    {
        var seed = await SeedAsync();
        var reservation = await AddReservationAsync(
            seed.FirstUnitId,
            seed.ActiveTenantId,
            ReservationStatus.Completed);

        var exception = await Assert.ThrowsAsync<BusinessException>(() => CreateService().CancelAsync(
            new CancelReservationInput(
                reservation.Id,
                "Test iptal nedeni",
                new ReservationAccessScopeInput([seed.FirstPropertyId], []))));

        Assert.Equal("RESERVATION_CANNOT_BE_CANCELLED", exception.Code);
        Assert.Equal(ReservationStatus.Completed, reservation.Status);
    }

    [Fact]
    public async Task Cancel_ShouldRejectConfirmedReservationWithApprovedPayment()
    {
        var seed = await SeedAsync();
        var reservation = await AddReservationAsync(
            seed.FirstUnitId,
            seed.ActiveTenantId,
            ReservationStatus.Confirmed);
        var charge = new Charge
        {
            TenantId = seed.ActiveTenantId,
            UnitId = seed.FirstUnitId,
            ReservationId = reservation.Id,
            PeriodStart = reservation.StartDate,
            PeriodEnd = reservation.EndDate,
            DueDate = reservation.EndDate.Date,
            ExpectedAmount = 200m,
            TotalAmount = 240m,
            PaidAmount = 240m,
            Status = ChargeStatus.Paid,
            SourceType = ChargeSourceType.Reservation
        };
        _context.Charges.Add(charge);
        await _context.SaveChangesAsync();

        var lineItem = await PaymentLineItemTestHelper.AddLineItemAsync(_context, charge);
        var storeAccountId = await PaymentLineItemTestHelper.CreateStoreAccountAsync(_context);
        var userId = await _context.Users.Select(user => user.Id).FirstAsync();
        _context.PaymentAllocations.Add(new PaymentAllocation
        {
            ChargeId = charge.Id,
            ChargeLineItemId = lineItem.Id,
            StoreAccountId = storeAccountId,
            CreatedByUserId = userId,
            ApprovedByUserId = userId,
            PaymentDate = new DateTime(2026, 1, 1),
            Amount = 240m,
            PaymentChannel = PaymentChannel.Eft,
            Status = PaymentStatus.Approved,
            ApprovalDate = new DateTime(2026, 1, 1)
        });
        await _context.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<BusinessException>(() => CreateService().CancelAsync(
            new CancelReservationInput(
                reservation.Id,
                "Test iptal nedeni",
                new ReservationAccessScopeInput([seed.FirstPropertyId], []))));

        Assert.Equal("RESERVATION_HAS_APPROVED_PAYMENT", exception.Code);
        Assert.Equal(ReservationStatus.Confirmed, reservation.Status);
        Assert.Equal(ChargeStatus.Paid, charge.Status);
    }

    [Fact]
    public async Task Cancel_ShouldCancelUnpaidReservationChargeInSameTransaction()
    {
        var seed = await SeedAsync();
        var reservation = await AddReservationAsync(
            seed.FirstUnitId,
            seed.ActiveTenantId,
            ReservationStatus.Confirmed);
        var service = CreateService();
        var scope = new ReservationAccessScopeInput([seed.FirstPropertyId], []);
        var chargeId = await service.TransferToChargeAsync(
            new TransferReservationToChargeInput(reservation.Id, scope));

        await service.CancelAsync(new CancelReservationInput(
            reservation.Id,
            "Toplantı iptal edildi.",
            scope));

        var charge = await _context.Charges.SingleAsync(item => item.Id == chargeId);
        Assert.Equal(ReservationStatus.Cancelled, reservation.Status);
        Assert.Equal(ChargeStatus.Cancelled, charge.Status);
        Assert.Contains("Toplantı iptal edildi.", charge.CancellationNote);
    }

    [Fact]
    public async Task Transfer_ShouldRejectOutsideScopeBeforeCreatingCharge()
    {
        var seed = await SeedAsync();
        var reservation = await AddReservationAsync(seed.FirstUnitId, seed.ActiveTenantId);

        var exception = await Assert.ThrowsAsync<BusinessException>(() => CreateService().TransferToChargeAsync(
            new TransferReservationToChargeInput(
                reservation.Id,
                new ReservationAccessScopeInput([], [seed.SecondUnitId]))));

        Assert.Equal("RESERVATION_OUT_OF_SCOPE", exception.Code);
        Assert.False(await _context.Charges.AnyAsync(charge => charge.ReservationId == reservation.Id));
    }

    [Fact]
    public async Task Transfer_ShouldCreateChargeWithoutChangingConfirmedStatus()
    {
        var seed = await SeedAsync();
        var reservation = await AddReservationAsync(seed.FirstUnitId, seed.ActiveTenantId);

        var chargeId = await CreateService().TransferToChargeAsync(
            new TransferReservationToChargeInput(
                reservation.Id,
                new ReservationAccessScopeInput([seed.FirstPropertyId], [])));

        Assert.True(chargeId > 0);
        Assert.Equal(ReservationStatus.Confirmed, reservation.Status);
        Assert.True(await _context.Charges.AnyAsync(charge =>
            charge.Id == chargeId && charge.ReservationId == reservation.Id));

        var duplicate = await Assert.ThrowsAsync<BusinessException>(() =>
            CreateService().TransferToChargeAsync(new TransferReservationToChargeInput(
                reservation.Id,
                new ReservationAccessScopeInput([seed.FirstPropertyId], []))));
        Assert.Equal("RESERVATION_ALREADY_TRANSFERRED", duplicate.Code);
        Assert.Equal(1, await _context.Charges.CountAsync(charge => charge.ReservationId == reservation.Id));
    }

    [Theory]
    [InlineData(ReservationStatus.PendingApproval)]
    [InlineData(ReservationStatus.Rejected)]
    [InlineData(ReservationStatus.Cancelled)]
    [InlineData(ReservationStatus.Completed)]
    public async Task Transfer_ShouldRejectNonConfirmedStatuses(ReservationStatus status)
    {
        var seed = await SeedAsync();
        var reservation = await AddReservationAsync(seed.FirstUnitId, seed.ActiveTenantId, status);

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            CreateService().TransferToChargeAsync(new TransferReservationToChargeInput(
                reservation.Id,
                new ReservationAccessScopeInput([seed.FirstPropertyId], []))));

        Assert.Equal("RESERVATION_NOT_CONFIRMED", exception.Code);
        Assert.False(await _context.Charges.AnyAsync(charge => charge.ReservationId == reservation.Id));
    }

    [Fact]
    public async Task Transfer_ShouldRejectFreeReservation()
    {
        var seed = await SeedAsync();
        var reservation = await AddReservationAsync(seed.FirstUnitId, seed.ActiveTenantId);
        reservation.TotalAmount = 0;
        reservation.RateAmount = 0;
        await _context.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            CreateService().TransferToChargeAsync(new TransferReservationToChargeInput(
                reservation.Id,
                new ReservationAccessScopeInput([seed.FirstPropertyId], []))));

        Assert.Equal("RESERVATION_FREE_TRANSFER", exception.Code);
        Assert.False(await _context.Charges.AnyAsync(charge => charge.ReservationId == reservation.Id));
    }

    [Fact]
    public async Task Attendee_ShouldRejectDuplicateNormalizedEmailWithinReservation()
    {
        var seed = await SeedAsync();
        var reservation = await AddReservationAsync(seed.FirstUnitId, seed.ActiveTenantId);
        _context.ReservationAttendees.AddRange(
            new ReservationAttendee
            {
                ReservationId = reservation.Id,
                DisplayName = "Birinci Katılımcı",
                EmailAddress = "participant@example.com",
                NormalizedEmailAddress = "PARTICIPANT@EXAMPLE.COM"
            },
            new ReservationAttendee
            {
                ReservationId = reservation.Id,
                DisplayName = "İkinci Katılımcı",
                EmailAddress = "Participant@example.com",
                NormalizedEmailAddress = "PARTICIPANT@EXAMPLE.COM"
            });

        await Assert.ThrowsAsync<DbUpdateException>(() => _context.SaveChangesAsync());
    }

    private ReservationService CreateService()
    {
        return new(new ReservationRepository(_context),
                   new ReservationRateOverrideRepository(_context),
                   new ChargeRepository(_context),
                   new ChargeTypeRepository(_context),
                   new UnitRepository(_context),
                   new TenantRepository(_context),
                   ReservationBusinessRulesTestFactory.Create(),
                   new UnitOfWork(_context),
                   new KiraTakip.Infrastructure.Persistence.EfCoreConcurrencyViolationDetector());
    }

    private async Task<ReservationSeedData> SeedAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var firstProperty = new Property { Name = $"Rezervasyon Taşınmaz 1 {suffix}" };
        var secondProperty = new Property { Name = $"Rezervasyon Taşınmaz 2 {suffix}" };
        var chargeType = new ChargeType
        {
            Name = $"Rezervasyon Borç Tipi {suffix}",
            Code = $"REZ_{suffix}",
            Behavior = ChargeTypeBehavior.ReservationSpecific
        };
        var ratedUnitType = new UnitType
        {
            Name = $"Rezervasyon Birim Türü {suffix}",
            Code = $"RBT_{suffix}",
            Usage = UnitTypeUsage.Reservable,
            ChargeType = chargeType
        };
        var noRateUnitType = new UnitType
        {
            Name = $"Tarifesiz Birim Türü {suffix}",
            Code = $"TBT_{suffix}",
            Usage = UnitTypeUsage.Reservable,
            ChargeType = chargeType
        };
        var activeTenant = new Tenant
        {
            TenantNo = $"REZ-A-{suffix}",
            Name = $"Aktif Rezervasyon Kiracısı {suffix}"
        };
        var inactiveTenant = new Tenant
        {
            TenantNo = $"REZ-P-{suffix}",
            Name = $"Pasif Rezervasyon Kiracısı {suffix}",
            IsActive = false
        };

        _context.AddRange(
            firstProperty,
            secondProperty,
            ratedUnitType,
            noRateUnitType,
            activeTenant,
            inactiveTenant);
        await _context.SaveChangesAsync();

        var firstUnit = CreateUnit(firstProperty.Id, ratedUnitType.Id, $"Salon 1 {suffix}");
        var secondUnit = CreateUnit(secondProperty.Id, ratedUnitType.Id, $"Salon 2 {suffix}");
        var outsideUnit = CreateUnit(secondProperty.Id, ratedUnitType.Id, $"Salon 3 {suffix}");
        var noRateUnit = CreateUnit(firstProperty.Id, noRateUnitType.Id, $"Tarifesiz Salon {suffix}");
        _context.Units.AddRange(firstUnit, secondUnit, outsideUnit, noRateUnit);
        await _context.SaveChangesAsync();

        _context.Set<ReservationRateOverride>().Add(new ReservationRateOverride
        {
            UnitTypeId = ratedUnitType.Id,
            Year = 2026,
            FreeDurationMinutes = 0,
            BillingPeriodMinutes = 60,
            PeriodRate = 100m,
            KdvRate = 20m
        });
        await _context.SaveChangesAsync();

        return new ReservationSeedData(
            firstProperty.Id,
            firstUnit.Id,
            secondUnit.Id,
            outsideUnit.Id,
            noRateUnit.Id,
            activeTenant.Id,
            inactiveTenant.Id);
    }

    private async Task<Reservation> AddReservationAsync(
        int unitId,
        int tenantId,
        ReservationStatus status = ReservationStatus.Confirmed)
    {
        var reservation = new Reservation
        {
            UnitId = unitId,
            TenantId = tenantId,
            StartDate = new DateTime(2026, 1, 1, 9, 0, 0),
            EndDate = new DateTime(2026, 1, 1, 11, 0, 0),
            TotalDurationMinutes = 120,
            PaidDurationMinutes = 120,
            UnitRate = 100m,
            RateAmount = 200m,
            TotalAmount = 240m,
            Status = status
        };
        _context.Reservations.Add(reservation);
        await _context.SaveChangesAsync();
        return reservation;
    }

    private static CreateReservationInput BuildCreateInput(
        int unitId,
        int tenantId,
        ReservationAccessScopeInput accessScope)
        => new(
            unitId,
            tenantId,
            new DateTime(2026, 1, 1, 9, 0, 0),
            new DateTime(2026, 1, 1, 11, 0, 0),
            "Test rezervasyonu",
            accessScope);

    private static CreateReservationRequestInput BuildRequestInput(
        int unitId,
        int tenantId,
        string userId,
        string email,
        ReservationAccessScopeInput accessScope,
        bool createAndApprove = false)
        => new(
            unitId,
            tenantId,
            new DateTime(2026, 1, 1, 9, 0, 0),
            new DateTime(2026, 1, 1, 11, 0, 0),
            "Mimari toplantısı",
            "Açıklama",
            "Not",
            null,
            [new ReservationAttendeePolicyInput("Katılımcı", "participant@example.com", false)],
            createAndApprove,
            userId,
            "Talep Sahibi",
            email,
            accessScope);

    private static Unit CreateUnit(int propertyId, int unitTypeId, string name)
        => new()
        {
            PropertyId = propertyId,
            UnitTypeId = unitTypeId,
            Name = name,
            Area = 20m
        };

    private sealed record ReservationSeedData(
        int FirstPropertyId,
        int FirstUnitId,
        int SecondUnitId,
        int OutsideUnitId,
        int NoRateUnitId,
        int ActiveTenantId,
        int InactiveTenantId);
}
