using RestSharp;
using BankWebApp.Models;

namespace BankWebApp.Services
{
    public class ApiService
    {
        private readonly RestClient _client;

        public ApiService(IConfiguration configuration) {

            var baseUrl = configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7045";
            _client = new RestClient(baseUrl);
        }

        // Get accounts for a specific user
        public async Task<List<Account>> getAccountsForUserAsync(int userId)
        {
            var request = new RestRequest($"api/accounts/user/{userId}", Method.Get);
            var response = await _client.ExecuteAsync<List<Account>>(request);
            return response.Data ?? new List<Account>();
        }

        // Get paginated and filtered transactions for an account
        public async Task<PagedResult<Transaction>> GetTransactionsAsync(

            int accountId, int page = 1, int pageSize = 10,
            string sortBy = "date", string sortOrder = "desc",
            DateTime? startDate = null, DateTime? endDate = null)
        {

            var request = new RestRequest($"api/transactions/account/{accountId}", Method.Get);
            request.AddQueryParameter("page", page);
            request.AddQueryParameter("pageSize", pageSize);
            request.AddQueryParameter("sortBy", sortBy);
            request.AddQueryParameter("sortOrder", sortOrder);

            if (startDate.HasValue) request.AddQueryParameter("startDate", startDate.Value.ToString("o"));
            if (endDate.HasValue) request.AddQueryParameter("endDate", endDate.Value.ToString("o"));

            var response = await _client.ExecuteAsync<PagedResult<Transaction>>(request);
            return response.Data ?? new PagedResult<Transaction>();

        }       
    }

    // Helper model for paginated responses
    public class PagedResult<T>
    {
        public int TotalRecords { get; set; }
        public int Page {  get; set; }
        public int PageSize { get; set; }
        public List<T> Data { get; set; }
    }
}
