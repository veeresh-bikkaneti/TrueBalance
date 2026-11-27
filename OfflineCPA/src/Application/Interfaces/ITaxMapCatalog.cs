using Application.Charts;

namespace Application.Interfaces;

public interface ITaxMapCatalog
{
    TaxMap Find(string chart, int taxYear);

    int LatestYear(string chart);

    /// <summary>Smallest payment that calls for a 1099 in the given tax year.</summary>
    decimal Form1099Threshold(int taxYear);
}
