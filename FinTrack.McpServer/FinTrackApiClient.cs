using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace FinTrack.McpServer;

public sealed class FinTrackApiClient : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private bool _isAuthenticated;

    public FinTrackApiClient(IConfiguration configuration)
    {
        var configuredBaseUrl =
            configuration["FinTrackApi:BaseUrl"]
            ?? "http://127.0.0.1:5074/";

        if (!Uri.TryCreate(
                configuredBaseUrl,
                UriKind.Absolute,
                out var baseUri)
            || (baseUri.Scheme != Uri.UriSchemeHttp
                && baseUri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException(
                "FinTrackApi:BaseUrl must be an absolute HTTP or HTTPS URL.");
        }

        var cookieContainer = new CookieContainer();

        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = false,
            CookieContainer = cookieContainer,
            UseCookies = true
        };

        _httpClient = new HttpClient(
            handler,
            disposeHandler: true)
        {
            BaseAddress = EnsureTrailingSlash(baseUri),
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    public string BaseUrl =>
        _httpClient.BaseAddress!.ToString();

    public bool IsAuthenticated =>
        _isAuthenticated;


    // =========================================================
    // LOGIN
    // =========================================================

    public async Task<FinTrackApiResult> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email)
            || string.IsNullOrWhiteSpace(password))
        {
            return FinTrackApiResult.ValidationError(
                "Email and password are required.");
        }

        var response = await SendAsync(
            () => _httpClient.PostAsJsonAsync(
                "api/auth/login",
                new LoginRequest(
                    email.Trim(),
                    password),
                JsonOptions,
                cancellationToken),
            cancellationToken);

        _isAuthenticated = response.Success;

        return response;
    }


    // =========================================================
    // GET EXPENSES
    // =========================================================

    public async Task<FinTrackApiResult> GetExpensesAsync(
        int months,
        CancellationToken cancellationToken)
    {
        if (!_isAuthenticated)
        {
            return FinTrackApiResult.ValidationError(
                "Sign in with fintrack_login before retrieving expenses.");
        }

        if (months < 1 || months > 12)
        {
            return FinTrackApiResult.ValidationError(
                "months must be between 1 and 12.");
        }

        var toDate = DateTime.Today;
        var fromDate = toDate.AddMonths(-months);

        var allTransactions =
            new List<JsonElement>();

        var currentPage = 1;
        const int pageSize = 100;

        while (true)
        {
            var url =
                "api/transactions" +
                $"?fromDate={Uri.EscapeDataString(fromDate.ToString("yyyy-MM-dd"))}" +
                $"&toDate={Uri.EscapeDataString(toDate.ToString("yyyy-MM-dd"))}" +
                "&type=Expense" +
                $"&page={currentPage}" +
                $"&pageSize={pageSize}";

            var response = await SendAsync(
                () => _httpClient.GetAsync(
                    url,
                    cancellationToken),
                cancellationToken);

            if (response.StatusCode is 401 or 403)
            {
                _isAuthenticated = false;
                return response;
            }

            if (!response.Success)
            {
                return response;
            }

            if (response.Data is not
                {
                    ValueKind: JsonValueKind.Object
                } data)
            {
                return FinTrackApiResult.ConnectionError(
                    "FinTrack returned an unexpected transaction response.");
            }

            if (!data.TryGetProperty(
                    "transactions",
                    out var transactionsElement)
                || transactionsElement.ValueKind !=
                    JsonValueKind.Array)
            {
                return FinTrackApiResult.ConnectionError(
                    "FinTrack transaction response does not contain a transactions array.");
            }

            foreach (var transaction in transactionsElement.EnumerateArray())
            {
                allTransactions.Add(
                    transaction.Clone());
            }

            var totalPages = 0;

            if (data.TryGetProperty(
                    "totalPages",
                    out var totalPagesElement)
                && totalPagesElement.ValueKind ==
                    JsonValueKind.Number)
            {
                totalPages =
                    totalPagesElement.GetInt32();
            }

            if (totalPages <= 0 ||
                currentPage >= totalPages)
            {
                break;
            }

            currentPage++;
        }

        var combinedData = new
        {
            fromDate = fromDate.ToString("yyyy-MM-dd"),
            toDate = toDate.ToString("yyyy-MM-dd"),
            type = "Expense",
            months,
            transactionCount = allTransactions.Count,
            transactions = allTransactions
        };

        return new FinTrackApiResult(
            true,
            200,
            $"Retrieved {allTransactions.Count} expense transactions.",
            JsonSerializer.SerializeToElement(
                combinedData,
                JsonOptions));
    }


    // =========================================================
    // CREATE PAYMENT
    // =========================================================

    public async Task<FinTrackApiResult> CreatePaymentAsync(
        int recipientUserId,
        decimal amount,
        int expenseCategoryId,
        string? description,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (!_isAuthenticated)
        {
            return FinTrackApiResult.ValidationError(
                "Sign in with fintrack_login before creating a payment.");
        }

        if (recipientUserId <= 0
            || expenseCategoryId <= 0
            || amount <= 0)
        {
            return FinTrackApiResult.ValidationError(
                "recipientUserId, expenseCategoryId, and amount must all be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey)
            || idempotencyKey.Trim().Length > 128)
        {
            return FinTrackApiResult.ValidationError(
                "idempotencyKey is required and must contain at most 128 characters.");
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "api/payments")
        {
            Content = JsonContent.Create(
                new PaymentRequest(
                    recipientUserId,
                    amount,
                    description,
                    expenseCategoryId),
                options: JsonOptions)
        };

        request.Headers.Add(
            "Idempotency-Key",
            idempotencyKey.Trim());

        var response = await SendAsync(
            () => _httpClient.SendAsync(
                request,
                cancellationToken),
            cancellationToken);

        if (response.StatusCode is 401 or 403)
        {
            _isAuthenticated = false;
        }

        return response;
    }


    // =========================================================
    // LOGOUT
    // =========================================================

    public async Task<FinTrackApiResult> LogoutAsync(
        CancellationToken cancellationToken)
    {
        var response = await SendAsync(
            () => _httpClient.PostAsync(
                "api/auth/logout",
                content: null,
                cancellationToken),
            cancellationToken);

        if (response.Success
            || response.StatusCode is 401 or 403)
        {
            _isAuthenticated = false;
        }

        return response;
    }


    // =========================================================
    // DISPOSE
    // =========================================================

    public void Dispose()
    {
        _httpClient.Dispose();
    }


    // =========================================================
    // HTTP
    // =========================================================

    private async Task<FinTrackApiResult> SendAsync(
        Func<Task<HttpResponseMessage>> send,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await send();

            var body =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);

            var data =
                TryReadJson(body);

            var message =
                GetMessage(data)
                ?? (
                    response.IsSuccessStatusCode
                        ? "Request completed."
                        : $"FinTrack returned HTTP {(int)response.StatusCode} ({response.ReasonPhrase})."
                );

            return new FinTrackApiResult(
                response.IsSuccessStatusCode,
                (int)response.StatusCode,
                message,
                data);
        }
        catch (HttpRequestException)
        {
            return FinTrackApiResult.ConnectionError(
                "Could not reach FinTrack. Start the application or set FinTrackApi__BaseUrl to its URL.");
        }
        catch (TaskCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return FinTrackApiResult.ConnectionError(
                "The FinTrack request timed out after 30 seconds.");
        }
    }


    // =========================================================
    // HELPERS
    // =========================================================

    private static Uri EnsureTrailingSlash(
        Uri uri) =>
        uri.AbsoluteUri.EndsWith('/')
            ? uri
            : new Uri(
                $"{uri.AbsoluteUri}/",
                UriKind.Absolute);


    private static JsonElement? TryReadJson(
        string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            using var document =
                JsonDocument.Parse(body);

            return document.RootElement.Clone();
        }
        catch (JsonException)
        {
            return null;
        }
    }


    private static string? GetMessage(
        JsonElement? data)
    {
        if (data is not
            {
                ValueKind: JsonValueKind.Object
            } value
            || !value.TryGetProperty(
                "message",
                out var message)
            || message.ValueKind !=
                JsonValueKind.String)
        {
            return null;
        }

        return message.GetString();
    }


    // =========================================================
    // REQUEST DTOs
    // =========================================================

    private sealed record LoginRequest(
        string Email,
        string Password);

    private sealed record PaymentRequest(
        int ToUserId,
        decimal Amount,
        string? Description,
        int ExpenseCategoryId);
}


// =============================================================
// RESULT
// =============================================================

public sealed record FinTrackApiResult(
    bool Success,
    int StatusCode,
    string Message,
    JsonElement? Data)
{
    public static FinTrackApiResult ValidationError(
        string message) =>
        new(
            false,
            400,
            message,
            null);

    public static FinTrackApiResult ConnectionError(
        string message) =>
        new(
            false,
            503,
            message,
            null);
}