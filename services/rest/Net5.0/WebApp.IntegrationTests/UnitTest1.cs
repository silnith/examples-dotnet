using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;

namespace WebApp.IntegrationTests
{
    [TestClass]
    public class UnitTest1
    {
        private WebApplicationFactory<Startup> WebApplicationFactory;

        [TestInitialize]
        public void SetUp()
        {
            WebApplicationFactory = new();
        }

        [TestCleanup]
        public void TearDown()
        {
            WebApplicationFactory?.Dispose();
            WebApplicationFactory = null;
        }

        [TestMethod]
        public async Task TestMethod1()
        {
            using HttpClient httpClient = WebApplicationFactory.CreateDefaultClient();

            using HttpRequestMessage httpRequestMessage = new(HttpMethod.Get, "/swagger/index.html")
            {
            };
            using HttpResponseMessage httpResponseMessage = await httpClient.SendAsync(httpRequestMessage);

            Assert.AreEqual(HttpStatusCode.OK, httpResponseMessage.StatusCode);
        }
    }
}
