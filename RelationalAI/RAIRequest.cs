using System;
using System.IO;
using System.Net.Http;
using System.ComponentModel;
using System.Reflection;
using NSec.Cryptography;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Web;

namespace Com.RelationalAI
{
    public enum RAIInfra {
        AWS,
        AZURE,
    }

    public enum RAIRegion {
        [Description("us-east")]
        US_EAST,
    }

    public class RAIRequest {
        public const string DEFAULT_SERVICE = "transaction";

        public RAICredentials Creds { get; }
        // verb, url, headers, content
        public HttpRequestMessage InnerReq { get; }
        public RAIRegion Region { get; }
        public string Service { get; }

        public RAIRequest(
            HttpRequestMessage innerReq,
            Connection conn,
            string service = DEFAULT_SERVICE
        ): this(
            innerReq,
            (conn is CloudConnection || conn is ManagementConnection) ? conn.Creds : null,
            (conn is CloudConnection || conn is ManagementConnection) ? conn.Region : Connection.DEFAULT_REGION,
            service)
        {
        }

        public RAIRequest(
            HttpRequestMessage innerReq,
            RAICredentials creds = null,
            RAIRegion region = Connection.DEFAULT_REGION,
            string service = DEFAULT_SERVICE
        )
        {
            this.Creds = creds;
            this.InnerReq = innerReq;
            this.Region = region;
            this.Service = service;
        }

        public void Sign(string[] includeHeaders = null, int debugLevel=1) {
            Sign(DateTime.UtcNow, includeHeaders, debugLevel);
        }

        public void Sign(DateTime t, string[] includeHeaders = null, int debugLevel=1) {
            if(includeHeaders == null) {
                includeHeaders = new string[]{"host", "content-type", "x-rai-date"};
            }
            if(this.Creds == null) return;

            // ISO8601 date/time strings for time of request
            string signatureDate = String.Format("{0:yyyyMMddTHHmmssZ}", t);
            string scopeDate = String.Format("{0:yyyyMMdd}", t);

            // Authentication scope
            string scope = string.Join("/", new string[]{
                scopeDate, EnumString.GetDescription(this.Region), this.Service, "rai01_request"
            });

            // SHA256 hash of content
            Sha256 shaw256HashAlgo = new Sha256();
            byte[] reqContent = InnerReq.Content.ReadAsByteArrayAsync().Result;
            byte[] sha256Hash = shaw256HashAlgo.Hash(reqContent);
            string contentHash = sha256Hash.ToHex();

            // HTTP headers
            InnerReq.Headers.Authorization = null;
            // Include "x-rai-date" in signed headers
            if (!InnerReq.Headers.Contains("x-rai-date"))
            {
                InnerReq.Headers.TryAddWithoutValidation("x-rai-date", signatureDate);
            }

            var allHeaders = InnerReq.Headers.Union(InnerReq.Content.Headers);

            // Sort and lowercase() Headers to produce canonical form
            string canonicalHeaders = string.Join(
                "\n",
                from header in allHeaders
                orderby header.Key.ToLower()
                where includeHeaders.Contains(header.Key.ToLower())
                select string.Format(
                    "{0}:{1}",
                    header.Key.ToLower(),
                    string.Join(",", header.Value).Trim()
                )
            );
            string signedHeaders = string.Join(
                ";",
                from header in allHeaders
                orderby header.Key.ToLower()
                where includeHeaders.Contains(header.Key.ToLower())
                select header.Key.ToLower()
            );

            // Sort Query String
            var parsedQuery = HttpUtility.ParseQueryString(InnerReq.RequestUri.Query);
            var parsedQueryDict = parsedQuery.AllKeys.SelectMany(
                parsedQuery.GetValues, (k, v) => new {key = k, value = v}
            );
            string query = string.Join(
                "&",
                from qparam in parsedQueryDict
                orderby qparam.key, qparam.value
                select string.Format(
                    "{0}={1}",
                    HttpUtility.UrlEncode(qparam.key),
                    HttpUtility.UrlEncode(qparam.value))
            );

            // Create hash of canonical request
            string canonicalForm = string.Format(
                "{0}\n{1}\n{2}\n{3}\n\n{4}\n{5}",
                InnerReq.Method,
                HttpUtility.UrlPathEncode(InnerReq.RequestUri.AbsolutePath),
                query,
                canonicalHeaders,
                signedHeaders,
                contentHash
            );

            if(debugLevel > 2) {
                Console.WriteLine("reqContent:");
                Console.WriteLine(System.Text.Encoding.Default.GetString(reqContent));
                Console.WriteLine("canonical_form:");
                Console.WriteLine(canonicalForm);
                Console.WriteLine();
            }

            sha256Hash = shaw256HashAlgo.Hash(Encoding.UTF8.GetBytes(canonicalForm));
            string canonicalHash = sha256Hash.ToHex();

            // Create and sign "String to Sign"
            string stringToSign = string.Format(
                "RAI01-ED25519-SHA256\n{0}\n{1}\n{2}", signatureDate, scope, canonicalHash
            );

            byte[] seed = Convert.FromBase64String(Creds.PrivateKey);

            // select the Ed25519 signature algorithm
            var algorithm = SignatureAlgorithm.Ed25519;

            // create a new key pair
            using var key = Key.Import(algorithm, seed, KeyBlobFormat.RawPrivateKey);

            // sign the data using the private key
            byte[] signature = algorithm.Sign(key, Encoding.UTF8.GetBytes(stringToSign));

            string sig = signature.ToHex();

            if(debugLevel > 2) {
                Console.WriteLine("string_to_sign:");
                Console.WriteLine(stringToSign);
                Console.WriteLine();
                Console.WriteLine("signature:");
                Console.WriteLine(sig);
                Console.WriteLine();
            }

            var authHeader = string.Format(
                "RAI01-ED25519-SHA256 Credential={0}/{1}, SignedHeaders={2}, Signature={3}",
                Creds.AccessKey,
                scope,
                signedHeaders,
                sig
            );

            InnerReq.Headers.TryAddWithoutValidation("Authorization", authHeader);
        }
    }
}
