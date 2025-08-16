using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;

namespace Story_Spoiler
{
    [TestFixture]
    public class StoryTests
    {
        private RestClient? client;
        private static string createdStoryId = string.Empty;
        private static readonly string baseURL = "https://d3s5nxhwblsjbi.cloudfront.net";

        private static string GetJwtToken(string userName, string password)
        {
            var loginClient = new RestClient(baseURL);
            var req = new RestRequest("/api/User/Authentication", Method.Post)
                .AddJsonBody(new { userName, password });
            var resp = loginClient.Execute(req);
            var json = JsonSerializer.Deserialize<JsonElement>(resp.Content!);
            return json.GetProperty("accessToken").GetString()!;
        }

        [OneTimeSetUp]
        public void Setup()
        {
            string token = GetJwtToken("ico1", "ico1ico1");
            var options = new RestClientOptions(baseURL)
            {
                Authenticator = new JwtAuthenticator(token)
            };
            client = new RestClient(options);
        }

        [Test, Order(1)]
        public void CreateStory_ShouldReturnCreated_AndId_AndSuccessMsg()
        {
            var body = new
            {
                title = "New Story " + Guid.NewGuid().ToString("N")[..6],
                description = "Test story description",
                url = ""
            };

            var request = new RestRequest("/api/Story/Create", Method.Post).AddJsonBody(body);
            var response = client!.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content!);
            createdStoryId = json.GetProperty("storyId").GetString()!;

            var msg = json.GetProperty("msg").GetString();
            Assert.That(msg, Does.Contain("Successfully created!").IgnoreCase);
        }

        [Test, Order(2)]
        public void EditStory_WithPUT_ShouldReturnOk_AndSuccessMsg()
        {
            var updated = new
            {
                title = "Edited Title " + DateTime.UtcNow.Ticks,
                description = "Edited description",
                url = ""
            };

            var request = new RestRequest($"/api/Story/Edit/{createdStoryId}", Method.Put).AddJsonBody(updated);
            var response = client!.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content!);
            var msg = json.GetProperty("msg").GetString();
            Assert.That(msg, Does.Contain("Successfully edited").IgnoreCase);
        }

        [Test, Order(3)]
        public void GetAllStories_ShouldReturnList()
        {
            var request = new RestRequest("/api/Story/All", Method.Get);
            var response = client!.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var stories = JsonSerializer.Deserialize<List<object>>(response.Content!);
            Assert.That(stories, Is.Not.Empty);
        }

        [Test, Order(4)]
        public void DeleteStory_ShouldReturnOk_AndDeletedMsg()
        {
            var request = new RestRequest($"/api/Story/Delete/{createdStoryId}", Method.Delete);
            var response = client!.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK).Or.EqualTo(HttpStatusCode.NoContent));

            if (response.StatusCode == HttpStatusCode.OK && !string.IsNullOrWhiteSpace(response.Content))
            {
                var json = JsonSerializer.Deserialize<JsonElement>(response.Content!);
                var msg = json.GetProperty("msg").GetString();
                Assert.That(msg, Does.Contain("Deleted successfully!").IgnoreCase);
            }
        }

        [Test, Order(5)]
        public void CreateStory_WithoutRequiredFields_ShouldReturnBadRequest()
        {
            var request = new RestRequest("/api/Story/Create", Method.Post).AddJsonBody(new { });
            var response = client!.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test, Order(6)]
        public void EditNonExistingStory_ShouldReturnNotFound_OrBadRequest()
        {
            string fakeId = Guid.NewGuid().ToString();
            var updated = new { title = "X", description = "Y", url = "" };

            var request = new RestRequest($"/api/Story/Edit/{fakeId}", Method.Put).AddJsonBody(updated);
            var response = client!.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound).Or.EqualTo(HttpStatusCode.BadRequest));

            if (!string.IsNullOrWhiteSpace(response.Content))
            {
                var json = JsonSerializer.Deserialize<JsonElement>(response.Content!);
                if (json.TryGetProperty("msg", out var msgProp))
                {
                    Assert.That(msgProp.GetString(), Does.Contain("No spoilers").IgnoreCase);
                }
                else
                {
                    Assert.That(response.Content, Is.Not.Empty);
                }
            }
        }

        [Test, Order(7)]
        public void DeleteNonExistingStory_ShouldReturnBadRequest_AndUnableToDeleteMsg()
        {
            string fakeId = Guid.NewGuid().ToString();

            var request = new RestRequest($"/api/Story/Delete/{fakeId}", Method.Delete);
            var response = client!.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content!);
            var msg = json.GetProperty("msg").GetString();
            Assert.That(msg, Does.Contain("Unable to delete this story spoiler!").IgnoreCase);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            client?.Dispose();
            client = null;
        }
    }
}
