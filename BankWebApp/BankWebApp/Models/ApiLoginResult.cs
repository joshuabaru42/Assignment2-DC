namespace BankWebApp.Models
{
    // =====================================================
    // MEMBER 1 - PART B REQUIREMENTS 1 AND 2
    // Result of authenticating through the Part A API.
    //
    // Keeps the authenticated identity together with
    // the API session credential returned by BankService.
    //
    // SECURITY:
    // The API cookie is sensitive authentication data.
    // It must remain server-side, must never be logged,
    // and must not be rendered in an MVC view.
    //
    // This model does not store or manage sessions itself.
    // =====================================================

    public class ApiLoginResult
    {
        public LoginResponse? User { get; set; }

        public string? ApiAuthCookie { get; set; }
    }
}
