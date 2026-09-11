namespace KiraTakip.Models.Dtos.Reservation;

public record FindReservationCompletionCandidatesInput(int BatchSize);

public record CompleteReservationInput(int ReservationId);
