# FinTrack MCP Server

This local MCP server gives an MCP client access to FinTrack's current HTTP API. It communicates over standard input/output, making it suitable for local clients such as Codex, VS Code, Claude Desktop, or Cursor.

## Tools

- `fintrack_list_api_operations` documents the allowlisted endpoint tools.
- `fintrack_get_connection_status` returns the configured application URL and sign-in state.
- `fintrack_login` calls `POST /api/auth/login` and retains the resulting cookie only in the running MCP process.
- `fintrack_create_payment` calls `POST /api/payments`. It requires a stable idempotency key and `confirmation: "SEND"`.
- `fintrack_logout` calls `POST /api/auth/logout` and clears the local session.

The server intentionally has no arbitrary URL, method, header, or body tool. Such a proxy would let untrusted tool input access services beyond FinTrack.

## Set up

1. Start FinTrack. The development HTTP URL is `http://127.0.0.1:5074/`.
2. Build the MCP server:

   ```powershell
   dotnet build D:\FinTrack\FinTrack.McpServer\FinTrack.McpServer.csproj
   ```

3. Add the built DLL as a stdio MCP server in your MCP client's configuration. Use `dotnet` as the command and the built DLL as its only argument. Do not use `dotnet run`: its build output can corrupt stdio MCP messages.

   ```json
   {
     "mcpServers": {
       "fintrack": {
         "command": "dotnet",
         "args": [
           "D:\\FinTrack\\FinTrack.McpServer\\bin\\Debug\\net10.0\\FinTrack.McpServer.dll"
         ],
         "env": {
           "FinTrackApi__BaseUrl": "http://127.0.0.1:5074/"
         }
       }
     }
   }
   ```

`FinTrackApi__BaseUrl` is optional when FinTrack uses the default local HTTP address. Set it to the exact deployed HTTPS URL when connecting to a non-local instance. Keep TLS certificate validation enabled.

## Payment safety

Use a unique idempotency key for each payment, and reuse the same key only when retrying that exact payment. The MCP server will not call the payment endpoint unless `confirmation` is exactly `SEND`. Review the recipient, category, and amount with the user immediately before that call.
