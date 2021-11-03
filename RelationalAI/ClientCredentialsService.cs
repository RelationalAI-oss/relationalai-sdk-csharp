using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Collections.Generic;
using NSec.Cryptography;
using System.Text;

namespace Com.RelationalAI
{
    /// <summary>Class <c>ClientCredentialsService</c> is used to get Access Token from authentication API for SDK access on RAICloud services.</summary>
    /// <remarks> It implements the singleton pattern to provide a single object to all the classes in the SDK. 
    /// It keeps a Dictionary based cache of Access Tokens. A dictionary has been used to enable the service to support multiple tenants/connections/clouds
    /// It keeps track of the token generation and expiration time and only grabs a new AccessToken when the cached Token is expired.
    /// Currently the cached and/or expired tokens are only evicted when the consumer will call the GetAccessToken method.
    /// </remarks>
   class ClientCredentialsService 
   {
        // Private constructor for singleton
        private ClientCredentialsService(){}
        
        // Singleton instance of ClientCredentialsService
        private static ClientCredentialsService instance;
        
        // Constants
        private const string ACCESS_TOKEN_KEY = "access_token";
        private const string EXPIRES_IN_KEY = "expires_in";
        private const string CLIENT_ID_KEY = "client_id";
        private const string CLIENT_SECRET_KEY = "client_secret";
        private const string AUDIENCE_KEY = "audience";
        private const string GRANT_TYPE_KEY = "grant_type";
        private const string CLIENT_CREDENTIALS_KEY = "client_credentials";

        // Locking object for GetInstance class
        private static readonly object syncLock = new object();
        
        // Authentication API URL Prefix to build the URI
        private static readonly string API_URL_PREFIX = "https://login";
        
        // Authentication API URL Postfix to build the URI
        private static readonly string API_URL_POSTFIX = ".relationalai.com/oauth/token";
        
        
        // Dictionary to hold Access Tokens. Using Dictionary to support multiple tenants/connections from the SDK.
        private Dictionary<string, AccessToken> accessTokenCache = new Dictionary<string, AccessToken>();
        
        /// <summary> Gets the singleton instance of <c>ClientCredentialsService</c> </summary>
        /// <remarks>Thread Safety Singleton using Double-Check Locking </remarks>
        /// <return> <c> ClientCredentialsService</c>.<return>
        public static ClientCredentialsService Instance
        {
            get 
            {
                if (instance == null) 
                {
                    lock (syncLock) 
                    {
                        if (instance == null) {
                            instance = new ClientCredentialsService();
                        }
                    }
                }
                return instance;
            }
        }

        /// <summary> Gets Access Token from authentication API. </summary>
        /// <example> For example:
        /// <code>
        ///    ClientCredentialsService.Instance.GetAccessToken(credentials, host);
        /// </code>
        /// results in <c>string</c> Access Token for SDK authentication.
        /// </example>
        /// <param name="credentials">RAICredentials Object. Contains ClientId and ClientSecret from ~/.rai/config</param>
        /// <param name="host">Host value from ~/.rai/config</param>
        /// <exception> Throws ClientCredentialsException if failed to get the access token from remote API. </exception>
        /// <remarks> This function will throw exception in the following scenarios
        /// 1. Client id and/or client secret is wrong.
        ///  2. Client id does not have permission on the API.
        /// 3. Access token generation quota has been exhausted.
        /// 4. Any network communication issue.
        /// 5. The remote API or the audience has been renamed or does not exist.
        /// 6. If the host-name/url is not in proper format. 
        /// </remarks>
        public string GetAccessToken(RAICredentials credentials, string host)
        {
            // Create the cache retrieval key.
            // It is a concatenation of client ID and audience for supporting 
            // a client with multiple domains.
            string cacheKey = GetCacheKey(credentials.ClientId, host);

            // Check if there is already a valid access token is present in the cache.
            AccessToken accessToken = GetValidAccessTokenFromCache(cacheKey);
            // If there is valid/un-expired token, then don't get a new one, just return the stored token.
            if(accessToken != null)
            {
                return accessToken.Token;
            } 
            string normalizedHostName = host.StartsWith("https://") ? host : ("https://" + host);
            // Get the new access token from the remote API.
            string apiResult = GetAccessTokenInternal(credentials.ClientId, credentials.ClientScrt, normalizedHostName, GetApiUriFromHost(host)).GetAwaiter().GetResult();
            // Convert the JSON result into a dictionary to grab the access token and expiration.
            Dictionary<string, string> result = (Dictionary<string, string>) Newtonsoft.Json.JsonConvert.DeserializeObject(apiResult, typeof(Dictionary<string, string>));
            if(result != null && result.Count > 0) 
            {
                // Add the Access Token object in the cache.
                accessTokenCache.Add(cacheKey, new AccessToken(result[ACCESS_TOKEN_KEY], long.Parse(result[EXPIRES_IN_KEY])));
                // Return the Access Token
                return result[ACCESS_TOKEN_KEY];
            }
            // Throw ClientCredentialsException because we have failed to get one.
            throw new ClientCredentialsException("Failed to get Access-Token from the remote API");
        }

        /// <summary> Removes a cached access token from the cache. </summary>
        /// <param name="credentials">RAICredentials Object. Contains ClientId and ClientSecret from ~/.rai/config</param>
        /// <param name="host">Host value from ~/.rai/config</param>
        public void InvalidateCache(RAICredentials credentials, string host)
        {
            if(credentials != null) 
            {
                string cacheKey = GetCacheKey(credentials.ClientId, host);
                // Do not need to verify if the key is successfully removed or not?
                // In case if the key is not then Remove will return false
                // This won't throw exception unless the key is null.
                accessTokenCache.Remove(cacheKey);
            }
        }

        /// <summary> Gets Access Token from authentication API.</summary>
        /// <param name="clientId">client_id as mentioned in the ~/.rai/config</param>
        /// <param name="clientSecret">client_secret value from ~/.rai/config</param>
        /// <param name="audience">The token token audience/target API (Machine to Machine Application API)</param>
        /// <param name="apiUrl">Auth token API endpoint.</param>
        /// <exception> Throws ClientCredentialsException if failed to get the access token from remote API. </exception>
        /// <remarks> This function will throw exception in the following scenarios,
        /// 1. Client id and/or client secret is wrong.
        //  2. Client id does not have permission on the API.
        /// 3. Access token generation quota has been exhausted.
        /// 4. Any network communication issue.
        /// 5. The remote API or the audience has been renamed or does not exist.
        /// </remarks>
        /// <return> Access token response as <c>string</c>.</return>
        private async Task<string> GetAccessTokenInternal(string clientId, string clientSecret, string audience, Uri apiUrl) 
        {
            // Form the API request body.
            string body = "{\"" + CLIENT_ID_KEY + "\":\""+ clientId + "\",\"" + CLIENT_SECRET_KEY + "\":\"" + clientSecret 
            + "\",\"" + AUDIENCE_KEY + "\":\"" + audience + "\",\"" + GRANT_TYPE_KEY + "\":\"" + CLIENT_CREDENTIALS_KEY + "\"}";

            //Define the content object
            var content = new System.Net.Http.StringContent(body);
            try
            {
                // Create HTTP client to send the POST request
                // Using block will destroy the HTTP client automatically
                using (var client = new HttpClient())
                {
                    // Set the API url
                    client.BaseAddress = apiUrl;
                    // Create the POST request
                    var request = new HttpRequestMessage(new HttpMethod("POST"), client.BaseAddress);
                    // Set content in the request.
                    request.Content = content;
                    // Set the content type.
                    request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
                    // Set the Accepted Media Type as the response.
                    request.Headers.Accept.Add(System.Net.Http.Headers.MediaTypeWithQualityHeaderValue.Parse("application/json"));
                    // Get the result back or throws an exception.
                    var result = await client.SendAsync(request);
                    return await result.Content.ReadAsStringAsync();
                }
            }
            catch(Exception e)
            {
                // Wrap exception as ClientCredentialsException and throw it.
                throw new ClientCredentialsException(e.Message, e);
            }
        }

        /// <summary>Gets a key to store AccessToken in the cache.</summary>
        /// <param name="clientID">client_id as mentioned in the ~/.rai/config</param>
        /// <param name="audience">host value from ~/.rai/config</param>
        /// <remarks>Key is the concatenation of client ID and audience fields</remarks>
        /// <return> Cache key as <c>string</c>.</return>
        private static string GetCacheKey(string clientID, string audience)
        {
            return String.Format("{0}:{1}", clientID, audience);
        }

        /// <summary> Gets a valid un-expired Access Token from the cache</summary>
        /// <param name="cacheKey">Cache Key</param>
        /// <return> <c>AccessToken</c> object if an un-expired token is present in the cache. Otherwise, will return Null. </return> 
        private AccessToken GetValidAccessTokenFromCache(string cacheKey)
        {
            if(accessTokenCache.ContainsKey(cacheKey))
            {
                AccessToken accessToken = accessTokenCache[cacheKey];
                if(!accessToken.IsExpired())
                {
                    return accessToken;
                }
                accessTokenCache.Remove(cacheKey);
            }
            return null;
        }

        /// <summary> Formulates the authentication API endpoint from the host value in ~/.rai/config </summary>
        /// <param name="host">Value of host as mentioned in the ~/.rai/config</param>
        /// <example>host=azure-env.relationalai.com </example>
        /// <exception>Will throw exception if the host name/FQDN is not properly defined.</exception>
        /// <remarks> 
        /// The Production API Url will be registered with authentication service as https://login.relationalai.com/auth/token
        /// Dev and/or staging API Urls will be registered as https://login-env.relationalai.com/oauth/token.
        /// This function will check for a -env in the host field. If the host is for some dev or stanging environment
        /// then it will return the API Url for the environment otherwise it will return the production API Url. 
        /// </remarks>
        /// <return> API Url as <c>Uri</c> object.</return>
        private static Uri GetApiUriFromHost(string host)
        {
            string environment = "";
            // Search for hyphen, which means the host is some dev or staging environment.
            // If hyphen is present then extract the environment name using IndexOf and Substring function
            // of the string class.
            if(host.Contains("-"))
            {
                int hyphenStart = host.IndexOf('-');
                int indexOfDot = host.IndexOf('.', hyphenStart + 1);
                if(indexOfDot >= 0)
                {
                    environment = host.Substring(hyphenStart + 1,  indexOfDot - (hyphenStart + 1));
                }
                else
                {
                    environment = host.Substring(hyphenStart + 1);
                }
            }

            // Return API Url for either production or for an environment.
            if(environment != "")
            {
                return new Uri(String.Format("{0}-{1}{2}", API_URL_PREFIX, environment, API_URL_POSTFIX));
            }
            
            return new Uri(String.Format("{0}{1}", API_URL_PREFIX, API_URL_POSTFIX));
        }
   }

    /// <summary> This class is used to store the AccessToken Object in the cache. </summary>
    class AccessToken
    {
        public string Token { get; }
        public long ExpiresIn { get; }
        public DateTime TimeAcquired { get; }

        public AccessToken(string accessToken, long expiresIn)
        {
            Token = accessToken;
            ExpiresIn = expiresIn;
            TimeAcquired = DateTime.Now;
        }

        /// <summary> Checks if a Token has been expired or not? </summary>
        public bool IsExpired()
        {
            TimeSpan timeSpan = DateTime.Now - TimeAcquired;
            return (long)timeSpan.TotalSeconds >= ExpiresIn;
        }
    }
}
