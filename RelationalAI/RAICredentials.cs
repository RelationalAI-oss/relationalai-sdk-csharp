using System;
using System.Collections.Generic;
using System.IO;
using IniParser.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Com.RelationalAI
{

    /*
        RAICredentials
    Keeps user's credentials: access key and private key or client id and client secret.
    # Fields
    - `accessKey`: Access key
    - `privateKey`: Private key // pragma: allowlist secret
    - `clientId`: Client Id
    - `clientScrt`: Client Secret
    - `authType`: Auth type used for current credentials. 
    */
    public class RAICredentials
    {
        public string AccessKey { get; }
        public string PrivateKey { get; } // pragma: allowlist secret
        public string ClientId { get; }
        public string ClientScrt { get; }
        public AuthType AuthType { get; }

        public RAICredentials(string accessKey, string privateKey)
        {
            this.AccessKey = accessKey;
            this.PrivateKey = privateKey; // pragma: allowlist secret
            this.AuthType = AuthType.ACCESS_KEY;
        }

        public RAICredentials(string clientId, string clientScrt, AuthType authType)
        {
            this.ClientId = clientId;
            this.ClientScrt = clientScrt;
            this.AuthType = authType;
        }

        /*
            RAICredentials(; path=dotRaiConfigPath(), profile="default")
        RAICredentials constructor. Reads the rAI config file from `path` and returns the
        credentials, associated with the given `[profile]`.
        # Keywords
        - `path=dotRaiConfigPath()`: Path to the rAI config file
            (`~/.rai/config` by default)
        - `profile="default"`: The [profile] to look for
        # Returns
        - `RAICredentials`
        # Throws
        - `SystemError`: If,
            1. the `path` doesn't exist
            2. the path to `private_key_filename` doesn't exist
        - `KeyError`: If,
            1. `[profile]` isn't there
            2. `[profile]` doesn't contain the `access_key` key
            3. `[profile]` doesn't contain the `private_key_filename` key
            4. the private key's Json file doesn't contain a private key
            5. client_id is not present if AuthType is CLIENT_CREDENTIALS.
            6. client_secret is not present if AuthType is CLIENT_CREDENTIALS.
        */
        public static RAICredentials FromFile(string path=null, string profile="default")
        {
            if( path == null) path = Config.DotRaiConfigPath();
            if(!File.Exists(path)) return null;
            IniData ini = Config.LoadDotRaiConfig(path);
            return RaiGetCredentials(ini, profile, pkDir:Path.GetDirectoryName(path));
        }

        /*
            raiGetCrData>
                RAICredentials
        Returns the credentials from the rAI config file, associated with the `[profile]`.
        # Arguments
        - `ini::Data)
        - `profile::AbstractString="default"`: The [profile] to look for
        # Keywords
        - `pkDir::AbstractString=dirname(dotRaiConfigPath())`: Path of the directory that
            contains private key's Json file
        # Returns
        - `RAICredentials`
        # Throws
        - `SystemException`: If the path to `private_key_filename` doesn't exist
        - `SystemException`: If more than one credentials found in the config
        - `KeyError`: If,
            1. `[profile]` isn't there
            2. `[profile]` doesn't contain the `access_key` or `client_id` key
            3. `[profile]` doesn't contain the `private_key_filename` key or `client_secret`
            4. the private key's Json file doesn't contain a private key
            5. client_id is not present if AuthType is CLIENT_CREDENTIALS.
            6. client_secret is not present if AuthType is CLIENT_CREDENTIALS.
        */
        public static RAICredentials RaiGetCredentials(
            IniData ini,
            string profile="default",
            string pkDir=null
        )
        {
            if(pkDir == null) pkDir = Path.GetDirectoryName(Config.DotRaiConfigPath());
            return _raiGetCredentials(ini, profile, pkDir);
        }

        /*
        Extracts and returns the access & the private key from the rAI config & the private key
        file respectively.
        Or         
        Returns the Client Credentials based client id and client secret.
        
        Access Key Based Credentials - Access key and private key. 
        
        Client Credentials - Client ID and client secret

        If none of the auth is found then it will throw KeyNotFoundException.
        
        Throws SystemException if more than one credentials found.

        Example ~/.rai/config:
        ```
        [default]
        region = us-east
        host = azure-dev.relationalai.com
        port = 443
        infra = AZURE
        access_key = [...]
        private_key_filename = default_privatekey.json # pragma: allowlist secret
        Or
        client_id = [...] - M2M application client_id
        client_secret = [...] - M2M application client_secret
        
        [aws]
        region = us-east
        host = 127.0.0.1
        port = 8443
        infra = AWS
        access_key = [...]
        private_key_filename = aws_privatekey.json # pragma: allowlist secret
        Or
        client_id = [...] - M2M application client_id
        client_secret = [...] - M2M application client secret
        ```
        `private_key_filename` points to a JSON file under ~/.rai. This function loads the JSON and
        extracts the private key format relevant for this SDK.
        */
        private static RAICredentials _raiGetCredentials(
            IniData ini,
            string profile,
            string pkDir
        )
        {
            List<RAICredentials> credentialsList = new List<RAICredentials>();
            
            RAICredentials credentials = ReadAccessKeyCredentials(ini, profile, pkDir);
            if(credentials != null)
            {
                credentialsList.Add(credentials);
            }
            
            credentials = ReadClientCredentials(ini, profile);
            if (credentials != null)
            {
                credentialsList.Add(credentials);
            }

            if(credentialsList.Count == 0) 
            {
                throw new KeyNotFoundException("access_key | client_id");
            }
            else if (credentialsList.Count > 1) 
            {
                throw new SystemException("multiple credentials found in the config");
            }

            return credentialsList[0];

        }

        /// <summary> Tries to get Access Key based credentials.
        /// It will check for access_key in the config file and if the key
        /// is not found then it will return null otherwise it will check for private key.
        /// </summary>
        /// <exception>
        /// In case of access_key in the config file, the private key will be mandatory. 
        /// Otherwise, it will throw <c><KeyNotFoundException</c>. 
        /// </exception>
        /// <return> Returns <c>RAICredentials</c> object or null. </return>
        private static RAICredentials ReadAccessKeyCredentials( 
            IniData ini,
            string profile,
            string pkDir)
        {
            string accessKey = Config.GetIniValue(ini, profile, "access_key");
            if(accessKey != "notfound")
            {
                string privateKeyFileName = Config.GetIniValue(ini, profile, "private_key_filename");
                if(privateKeyFileName == "notfound")
                {
                    throw new KeyNotFoundException("private_key_filename");
                }
                
                string privateKeyFile = Path.Combine(pkDir, privateKeyFileName);
                if (!File.Exists(privateKeyFile))
                {
                    throw new SystemException("opening file $(repr(privateKeyFile))");
                }
                
                JsonTextReader reader = new JsonTextReader(new StreamReader(privateKeyFile));
                JObject json = JObject.Load(reader);
                string privateKey = json.GetValue("sodium")["seed"].ToString();

                return new RAICredentials(accessKey, privateKey);
            }
            return null;
        }

        /// <summary> Tries to get client credentials based auth params.
        /// It will check for client_id in the config file and if the key
        /// is not found then it will return null otherwise it will check for client secret.
        /// </summary>
        /// <exception>
        /// In case of client_id in the config file, the client_secret will be mandatory. 
        /// Otherwise, it will throw <c><KeyNotFoundException</c>. 
        /// </exception>
        /// <return> Returns <c>RAICredentials</c> object or null.  </return>
         private static RAICredentials ReadClientCredentials( 
            IniData ini,
            string profile)
        {
            string clientId = Config.GetIniValue(ini, profile, "client_id");
            if(clientId != "notfound")
            {
                string clientScrt = Config.GetIniValue(ini, profile, "client_secret");
                if(clientScrt == "notfound"){
                    throw new KeyNotFoundException("client_secret");
                }

                return new RAICredentials(clientId, clientScrt, AuthType.CLIENT_CREDENTIALS);
            }
            return null;
        }

    }
}