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
        public const string OrderedDelivery = "orderedDelivery";
        /// <summary>감기·허리·점심 거르기 등 건강을 방치한 회차. 입원 실패 경로 (R-QA-04).</summary>
        public const string NeglectedHealth = "neglectedHealth";
        /// <summary>야근 완수·회식 참석 등 승진 트랙. 승진 후보 엔딩 (R-QA-04).</summary>
        public const string PromotionTrack = "promotionTrack";
        /// <summary>동료와 가까워짐. 커버·부탁 연쇄 (R-QA-07).</summary>
        public const string CloseWithCoworker = "closeWithCoworker";
        /// <summary>연애 중. 기념일·관계 유지 사건 (R-QA-07).</summary>
        public const string Dating = "dating";
        /// <summary>사수·멘토 라인. 조언·부탁 연쇄 (R-QA-07).</summary>
        public const string MentorBond = "mentorBond";
        /// <summary>이웃 갈등. 화해 또는 지속 스트레스 (R-QA-07).</summary>
        public const string NeighborFeud = "neighborFeud";
        /// <summary>가족 연락이 이어진 회차. 부조·방문 후속 (R-QA-07).</summary>
        public const string FamilySupport = "familySupport";
    }
}
