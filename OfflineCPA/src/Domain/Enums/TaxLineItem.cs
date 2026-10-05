namespace Domain.Enums;

public enum TaxLineItem
{
    None = 0,
    GrossReceiptsOrSales = 1, // 1120-S Line 1a
    CostOfGoodsSold = 2, // 1120-S Line 2
    CompensationOfOfficers = 7, // 1120-S Line 7
    SalariesAndWages = 8, // 1120-S Line 8
    RepairsAndMaintenance = 9, // 1120-S Line 9
    BadDebts = 10, // 1120-S Line 10
    Rents = 11, // 1120-S Line 11
    TaxesAndLicenses = 12, // 1120-S Line 12
    Interest = 13, // 1120-S Line 13
    Depreciation = 14, // 1120-S Line 14
    Depletion = 15, // 1120-S Line 15
    Advertising = 16, // 1120-S Line 16
    PensionProfitSharing = 17, // 1120-S Line 17
    EmployeeBenefitPrograms = 18, // 1120-S Line 18
    OtherDeductions = 19, // 1120-S Line 19
    Travel = 20, // Common sub-item of 19
    Meals = 21, // Common sub-item of 19 (subject to 50% usually)
    Cash = 101, // Sch L Line 1
    AccountsReceivable = 102, // Sch L Line 2a
    Inventories = 103, // Sch L Line 3
    OtherCurrentAssets = 106, // Sch L Line 6
    LoansToShareholders = 107, // Sch L Line 7
    MortgageAndRealEstateLoans = 108, // Sch L Line 8
    OtherInvestments = 109, // Sch L Line 9
    BuildingsAndOtherDepreciableAssets = 110, // Sch L Line 10a
    IntangibleAssets = 112, // Sch L Line 12a
    OtherAssets = 114, // Sch L Line 14
    AccountsPayable = 116, // Sch L Line 16
    MortgagesNotesPayableCurrent = 117, // Sch L Line 17
    OtherCurrentLiabilities = 118, // Sch L Line 18
    LoansFromShareholders = 119, // Sch L Line 19
    MortgagesNotesPayableLongTerm = 120, // Sch L Line 20
    OtherLiabilities = 121, // Sch L Line 21
    CapitalStock = 122, // Sch L Line 22
    AdditionalPaidInCapital = 123, // Sch L Line 23
    RetainedEarnings = 124, // Sch L Line 24
    Distributions = 200 // Sch M-2 or K-1
}
