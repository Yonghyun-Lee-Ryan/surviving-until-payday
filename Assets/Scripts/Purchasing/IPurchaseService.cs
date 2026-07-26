using System;

namespace SurviveUntilPayday.Purchasing
{
    public static class PurchaseProductIds
    {
        /// <summary>전면 광고만 제거. 보상형은 유지.</summary>
        public const string RemoveInterstitial = "remove_interstitial";
    }

    public enum PurchaseStatus
    {
        Success = 0,
        Cancelled = 1,
        Failed = 2,
        AlreadyOwned = 3,
        NotSupported = 4
    }

    public readonly struct PurchaseResult
    {
        public PurchaseStatus Status { get; }
        public string ProductId { get; }
        public string Message { get; }

        public bool IsSuccess => Status == PurchaseStatus.Success || Status == PurchaseStatus.AlreadyOwned;

        public PurchaseResult(PurchaseStatus status, string productId, string message = null)
        {
            Status = status;
            ProductId = productId ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public static PurchaseResult Ok(string productId) =>
            new PurchaseResult(PurchaseStatus.Success, productId, "ok");

        public static PurchaseResult Owned(string productId) =>
            new PurchaseResult(PurchaseStatus.AlreadyOwned, productId, "already owned");

        public static PurchaseResult Fail(string productId, string message) =>
            new PurchaseResult(PurchaseStatus.Failed, productId, message);
    }

    /// <summary>
    /// 인앱 구매 추상화. 실제 스토어 SDK는 이후 연결한다.
    /// </summary>
    public interface IPurchaseService
    {
        bool IsOwned(string productId);

        void Purchase(string productId, Action<PurchaseResult> onFinished);
    }

    /// <summary>
    /// 에디터·테스트용. 즉시 성공하며 소유 여부를 메모리에 보관한다.
    /// </summary>
    public sealed class MockPurchaseService : IPurchaseService
    {
        private bool forceFail;
        private string failMessage = "mock fail";
        private readonly System.Collections.Generic.HashSet<string> owned =
            new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);

        public int PurchaseAttemptCount { get; private set; }

        public void SetOwned(string productId, bool value = true)
        {
            if (string.IsNullOrWhiteSpace(productId))
            {
                return;
            }

            if (value)
            {
                owned.Add(productId);
            }
            else
            {
                owned.Remove(productId);
            }
        }

        public void SetForceFail(bool fail, string message = null)
        {
            forceFail = fail;
            failMessage = string.IsNullOrWhiteSpace(message) ? "mock fail" : message;
        }

        public bool IsOwned(string productId)
        {
            return !string.IsNullOrWhiteSpace(productId) && owned.Contains(productId);
        }

        public void Purchase(string productId, Action<PurchaseResult> onFinished)
        {
            if (onFinished == null)
            {
                throw new ArgumentNullException(nameof(onFinished));
            }

            PurchaseAttemptCount++;
            if (string.IsNullOrWhiteSpace(productId))
            {
                onFinished(PurchaseResult.Fail(productId, "productId empty"));
                return;
            }

            if (forceFail)
            {
                onFinished(PurchaseResult.Fail(productId, failMessage));
                return;
            }

            if (owned.Contains(productId))
            {
                onFinished(PurchaseResult.Owned(productId));
                return;
            }

            owned.Add(productId);
            onFinished(PurchaseResult.Ok(productId));
        }
    }
}
