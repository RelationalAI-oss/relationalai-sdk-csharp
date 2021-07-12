using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Linq;
using NUnit.Framework;

namespace Com.RelationalAI
{
    public class LocalIntegrationTests
    {

        [SetUp]
        public void Setup()
        {
        }

        public static LocalConnection CreateLocalConnection() {
            string dbname = IntegrationTestsCommons.genDbname("testcsharpclient");

            return CreateLocalConnection(dbname);
        }

        public static LocalConnection CreateLocalConnection(string dbname) {
            return new LocalConnection(dbname);
        }

        [Test]
        public void Test1()
        {
            IntegrationTestsCommons.RunAllTests(CreateLocalConnection);
        }

        [Test]
        public void Test2()
        {
            string dbname = IntegrationTestsCommons.genDbname("testcsharpclient");

            CloudConnection cloudConn = new CloudConnection(
                new LocalConnection(dbname),
                creds: new RAICredentials(
                    "e3536f8d-cbc6-4ed8-9de6-74cf4cb724a1",
                    "krnXRBoE0lX6NddvryxKIE+7RWrkWg6xk8NcGaSOdCo="
                ),
                verifySSL: false
            );

            HttpRequestMessage httpReq = new HttpRequestMessage();
            httpReq.Method = HttpMethod.Get;
            httpReq.RequestUri = new Uri("https://127.0.0.1:8443/database");
            httpReq.Content = new StringContent("{}");
            httpReq.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            httpReq.Headers.Host = "127.0.0.1";
            RAIRequest req = new RAIRequest(httpReq, cloudConn, service: "database+list");

            req.Sign(DateTime.Parse("2020-05-04T10:36:00"), debugLevel: cloudConn.DebugLevel);

            string output = string.Join(
                "\n",
                from header in req.InnerReq.Headers.Union(req.InnerReq.Content.Headers)
                orderby header.Key.ToLower()
                select string.Format(
                    "{0}: {1}",
                    header.Key.ToLower(),
                    string.Join(",", header.Value).Trim()
                )
            )+"\n";
            string expected =
                "authorization: RAI01-ED25519-SHA256 " +
                "Credential=e3536f8d-cbc6-4ed8-9de6-74cf4cb724a1/20200504/us-east/database+list/rai01_request, " +
                "SignedHeaders=content-type;host;x-rai-date, " +
                "Signature=77d211417454ded42dc931d25c57af6cab6cbc70f75bef4c849d37585188d659158c8c944eab866e3147bbcde21257ae0a1dfece3c0f3f43a838b3f9524e0f0a\n" +
                "content-type: application/json\n" +
                "host: 127.0.0.1\n" +
                "x-rai-date: 20200504T103600Z\n";

            Assert.AreEqual(output, expected);
        }
    }
}
