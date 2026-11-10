using System.Globalization;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;

namespace QuanLySieuThi.Service
{
    public sealed class MomoGateway(HttpClient http, IConfiguration configuration)
    {
        private string? PartnerCode => configuration["MoMo:PartnerCode"];
        private string? AccessKey => configuration["MoMo:AccessKey"];
        private string? SecretKey => configuration["MoMo:SecretKey"];
        private string Host => configuration.GetValue<bool>("MoMo:Production")
            ? "https://payment.momo.vn" : "https://test-payment.momo.vn";
        private Uri? PublicUrl => Uri.TryCreate(configuration["MoMo:PublicUrl"], UriKind.Absolute, out var url)
            && url.Scheme == "https" ? url : null;

        public bool Configured => !string.IsNullOrWhiteSpace(PartnerCode)
            && !string.IsNullOrWhiteSpace(AccessKey) && !string.IsNullOrWhiteSpace(SecretKey) && PublicUrl is not null;

        public async Task<string?> CreateAsync(string orderId, long amount, CancellationToken cancellationToken)
        {
            var redirectUrl = new Uri(PublicUrl!, "/Sales/Invoices").AbsoluteUri;
            var ipnUrl = new Uri(PublicUrl!, "/payments/momo/ipn").AbsoluteUri;
            const string requestType = "captureWallet";
            var orderInfo = $"Thanh toan {orderId}";
            var signature = Sign(FormattableString.Invariant(
                $"accessKey={AccessKey}&amount={amount}&extraData=&ipnUrl={ipnUrl}&orderId={orderId}&orderInfo={orderInfo}&partnerCode={PartnerCode}&redirectUrl={redirectUrl}&requestId={orderId}&requestType={requestType}"));

            using var response = await http.PostAsJsonAsync(Host + "/v2/gateway/api/create", new
            {
                partnerCode = PartnerCode,
                requestId = orderId,
                amount,
                orderId,
                orderInfo,
                redirectUrl,
                ipnUrl,
                requestType,
                extraData = "",
                autoCapture = true,
                lang = "vi",
                signature
            }, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            var result = await response.Content.ReadFromJsonAsync<MomoResult>(cancellationToken);
            return result is { ResultCode: 0 } && result.OrderId == orderId && result.Amount == amount
                && Uri.TryCreate(result.PayUrl, UriKind.Absolute, out var link) && link.Scheme == "https"
                && (link.Host == "momo.vn" || link.Host.EndsWith(".momo.vn", StringComparison.Ordinal))
                ? link.AbsoluteUri : null;
        }

        public async Task<MomoResult?> QueryAsync(string orderId, CancellationToken cancellationToken)
        {
            var requestId = Guid.NewGuid().ToString("N");
            var signature = Sign($"accessKey={AccessKey}&orderId={orderId}&partnerCode={PartnerCode}&requestId={requestId}");
            using var response = await http.PostAsJsonAsync(Host + "/v2/gateway/api/query",
                new { partnerCode = PartnerCode, requestId, orderId, lang = "vi", signature }, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            var result = await response.Content.ReadFromJsonAsync<MomoResult>(cancellationToken);
            return result?.PartnerCode == PartnerCode && result?.OrderId == orderId ? result : null;
        }

        public bool Verify(MomoResult result)
        {
            if (!Configured || result.PartnerCode != PartnerCode || result.RequestId != result.OrderId
                || result.Signature is not { Length: 64 })
                return false;

            var expected = Sign(FormattableString.Invariant(
                $"accessKey={AccessKey}&amount={result.Amount}&extraData={result.ExtraData}&message={result.Message}&orderId={result.OrderId}&orderInfo={result.OrderInfo}&orderType={result.OrderType}&partnerCode={result.PartnerCode}&payType={result.PayType}&requestId={result.RequestId}&responseTime={result.ResponseTime}&resultCode={result.ResultCode}&transId={result.TransId}"));
            return CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(expected),
                Encoding.ASCII.GetBytes(result.Signature.ToLowerInvariant()));
        }

        private string Sign(string value) => Convert.ToHexStringLower(
            HMACSHA256.HashData(Encoding.UTF8.GetBytes(SecretKey!), Encoding.UTF8.GetBytes(value)));
    }

    public sealed record MomoResult(
        string? PartnerCode, string? OrderId, string? RequestId, long Amount, int? ResultCode,
        long TransId, string? ExtraData, string? Message, string? OrderInfo, string? OrderType,
        string? PayType, long ResponseTime, string? Signature, string? PayUrl);
}
