namespace SurviveUntilPayday.Data
{
    /// <summary>
    /// 회차 단위 플래그 id. EventCondition / EndingCondition / Choice·Outcome에서 사용한다.
    /// </summary>
    public static class RunFlags
    {
        public const string HasBoughtStock = "hasBoughtStock";
        public const string StockBigWin = "stockBigWin";
        public const string PhoneStillCracked = "phoneStillCracked";
        public const string OwesDebt = "owesDebt";
    }
}
