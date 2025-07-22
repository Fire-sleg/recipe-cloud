namespace IntegrationTests
{
    using System.Net;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Text.Json;
    using Xunit;
    using Microsoft.AspNetCore.Mvc.Testing;

    public class RecipeServiceAuthIntegrationTests : IClassFixture<WebApplicationFactory<AuthService.Program>>
    {
        private readonly HttpClient _client;

        public RecipeServiceAuthIntegrationTests(WebApplicationFactory<AuthService.Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetRecipes_WithValidToken_ReturnsOk()
        {
            // 🔐 Авторизація через AuthService
            var authPayload = new
            {
                Email = "testuser@example.com",
                Password = "SecurePassword123"
            };

            var authContent = new StringContent(
                JsonSerializer.Serialize(authPayload),
                Encoding.UTF8,
                "application/json"
            );

            var authResponse = await _client.PostAsync("http://localhost:5001/api/auth/login", authContent);
            authResponse.EnsureSuccessStatusCode();

            var authBody = await authResponse.Content.ReadAsStringAsync();
            var token = JsonSerializer.Deserialize<AuthResponse>(authBody)?.Token;

            // 🧾 Виклик RecipeService з токеном
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await _client.GetAsync("/api/recipes");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        private class AuthResponse
        {
            public string? Token { get; set; }
        }
    }

}