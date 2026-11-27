namespace Domain.Entities;

/// <summary>One business trip, kept as a record for a CPA.</summary>
public class MileageLogEntry
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public DateOnly Date { get; private set; }
    public string Purpose { get; private set; } = string.Empty;
    public decimal Miles { get; private set; }
    public decimal StartOdometer { get; private set; }
    public decimal EndOdometer { get; private set; }

    private MileageLogEntry()
    {
    }

    public static MileageLogEntry Create(DateOnly date, string purpose, decimal miles, decimal startOdometer, decimal endOdometer)
    {
        if (string.IsNullOrWhiteSpace(purpose))
        {
            throw new ArgumentException("A trip needs a purpose.", nameof(purpose));
        }

        if (startOdometer < 0 || miles <= 0 || miles > endOdometer - startOdometer)
        {
            throw new ArgumentException("Miles must be above zero and fit between the odometer readings.", nameof(miles));
        }

        return new MileageLogEntry
        {
            Date = date,
            Purpose = purpose.Trim(),
            Miles = miles,
            StartOdometer = startOdometer,
            EndOdometer = endOdometer
        };
    }
}
