using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace FinTrack.McpServer;

[McpServerToolType]
public sealed class FinTrackTools(
    FinTrackApiClient apiClient)
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        };


    // =========================================================
    // LIST API OPERATIONS
    // =========================================================

    [McpServerTool(
        Name = "fintrack_list_api_operations")]
    [Description(
        "Lists the FinTrack operations exposed through this MCP server and their required inputs.")]
    public string ListApiOperations() =>
        Serialize(new
        {
            baseUrl = apiClient.BaseUrl,

            operations = new object[]
            {
                new
                {
                    tool = "fintrack_login",
                    endpoint = "POST /api/auth/login",
                    authentication =
                        "No existing session required"
                },

                new
                {
                    tool = "fintrack_get_connection_status",
                    endpoint = "No HTTP request",
                    authentication =
                        "No authentication required"
                },

                new
                {
                    tool = "fintrack_get_expenses",
                    endpoint = "GET /api/transactions",
                    authentication =
                        "Authenticated FinTrack session required",
                    purpose =
                        "Retrieves the user's raw expense transactions so Claude can analyze spending and suggest a budget."
                },

                new
                {
                    tool = "fintrack_create_payment",
                    endpoint = "POST /api/payments",
                    authentication =
                        "Authenticated FinTrack session required",
                    safeguards =
                        "Requires idempotencyKey and confirmation exactly equal to SEND"
                },

                new
                {
                    tool = "fintrack_logout",
                    endpoint = "POST /api/auth/logout",
                    authentication =
                        "Uses the current in-memory session"
                }
            },

            note =
                "This server deliberately does not provide an unrestricted HTTP proxy. " +
                "The FinTrack URL is configured by the server owner, not by tool input."
        });


    // =========================================================
    // CONNECTION STATUS
    // =========================================================

    [McpServerTool(
        Name = "fintrack_get_connection_status")]
    [Description(
        "Returns the configured FinTrack URL and whether this MCP process currently has an authenticated FinTrack session. No network request is made.")]
    public string GetConnectionStatus() =>
        Serialize(new
        {
            baseUrl = apiClient.BaseUrl,
            isAuthenticated =
                apiClient.IsAuthenticated
        });


    // =========================================================
    // LOGIN
    // =========================================================

    [McpServerTool(
        Name = "fintrack_login")]
    [Description(
        "Signs this local MCP server into FinTrack. The session cookie is retained only in this MCP process and is never returned.")]
    public async Task<string> LoginAsync(
        [Description(
            "The FinTrack account email address.")]
        string email,

        [Description(
            "The FinTrack account password. It is sent only to the configured FinTrack server and is not logged or returned.")]
        string password,

        CancellationToken cancellationToken)
    {
        var result =
            await apiClient.LoginAsync(
                email,
                password,
                cancellationToken);

        return Serialize(
            result with
            {
                Data = result.Success
                    ? result.Data
                    : null
            });
    }


    // =========================================================
    // GET EXPENSES
    // =========================================================

    [McpServerTool(
        Name = "fintrack_get_expenses")]
    [Description("""
Retrieves the user's actual expense transactions from FinTrack.

This tool is READ-ONLY.

The tool retrieves raw transaction data. Claude must perform
the financial analysis itself.

Use this tool when the user asks about:
- Spending history
- Expense history
- Spending patterns
- Category-wise spending
- Monthly spending
- Spending trends
- Budget recommendations based on previous spending

Claude should analyze the returned transactions by:
- Grouping expenses by category.
- Calculating total spending by category.
- Calculating monthly spending.
- Calculating monthly averages.
- Comparing spending across months.
- Identifying increasing or decreasing spending.
- Identifying unusually high spending.
- Identifying categories where spending is consistently high.
- Recommending a reasonable budget based on the observed spending.

IMPORTANT:
Do not ask FinTrack to calculate the budget recommendation.
Claude performs the analysis and recommendation.

Do not modify the user's budget using this tool.
""")]
    public async Task<string> GetExpensesAsync(
        [Description(
            "Number of previous months of expense history to retrieve. Must be between 1 and 12. Defaults to 3.")]
        int months = 3,

        CancellationToken cancellationToken = default)
    {
        if (months < 1 || months > 12)
        {
            return Serialize(
                FinTrackApiResult.ValidationError(
                    "Months must be between 1 and 12."));
        }

        var result =
            await apiClient.GetExpensesAsync(
                months,
                cancellationToken);

        return Serialize(result);
    }


    // =========================================================
    // CREATE PAYMENT
    // =========================================================

    [McpServerTool(
        Name = "fintrack_create_payment")]
    [Description(
        "Creates a manual FinTrack payment. Call only after the user has confirmed the final recipient, category, and amount.")]
    public async Task<string> CreatePaymentAsync(
        [Description(
            "The existing FinTrack user ID that will receive the payment.")]
        int recipientUserId,

        [Description(
            "The positive payment amount.")]
        decimal amount,

        [Description(
            "The existing FinTrack expense category ID for the sender.")]
        int expenseCategoryId,

        [Description(
            "A caller-generated stable key, at most 128 characters, reused if this exact payment call is retried.")]
        string idempotencyKey,

        [Description(
            "Safety confirmation. This must be exactly SEND before the payment endpoint is called.")]
        string confirmation,

        CancellationToken cancellationToken,

        [Description(
            "Optional note to include with the payment.")]
        string? description = null)
    {
        if (!string.Equals(
                confirmation,
                "SEND",
                StringComparison.Ordinal))
        {
            return Serialize(
                FinTrackApiResult.ValidationError(
                    "Payment was not sent. Set confirmation to exactly SEND only after the user confirms the final details."));
        }

        var result =
            await apiClient.CreatePaymentAsync(
                recipientUserId,
                amount,
                expenseCategoryId,
                description,
                idempotencyKey,
                cancellationToken);

        return Serialize(result);
    }


    // =========================================================
    // LOGOUT
    // =========================================================

    [McpServerTool(
        Name = "fintrack_logout")]
    [Description(
        "Signs the current local MCP process out of FinTrack and clears its in-memory session.")]
    public async Task<string> LogoutAsync(
        CancellationToken cancellationToken)
    {
        var result =
            await apiClient.LogoutAsync(
                cancellationToken);

        return Serialize(
            result with
            {
                Data = null
            });
    }


    // =========================================================
    // SERIALIZATION
    // =========================================================

    private static string Serialize<T>(
        T value) =>
        JsonSerializer.Serialize(
            value,
            JsonOptions);
}