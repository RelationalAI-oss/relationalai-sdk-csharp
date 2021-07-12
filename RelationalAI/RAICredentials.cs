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

    Keeps user's credentials: access key and private key.

    # Fields
    - `accessKey`: Access key
    - `privateKey`: Private key // pragma: allowlist secret
    */
    public class RAICredentials
    {
        public string AccessKey { get; }
        public string PrivateKey { get; } // pragma: allowlist secret

        public RAICredentials(string accessKey, string privateKey)
        {
            this.AccessKey = accessKey;
            this.PrivateKey = privateKey; // pragma: allowlist secret
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
        - `SystemError`: If the path to `private_key_filename` doesn't exist
        - `KeyError`: If,
            1. `[profile]` isn't there
            2. `[profile]` doesn't contain the `access_key` key
            3. `[profile]` doesn't contain the `private_key_filename` key
            4. the private key's Json file doesn't contain a private key
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

        Example ~/.rai/config:
        ```
        [default]
        region = us-east
        host = azure-dev.relationalai.com
        port = 443
        infra = AZURE
        access_key = [...]
        private_key_filename = default_privatekey.json # pragma: allowlist secret

        [aws]
        region = us-east
        host = 127.0.0.1
        port = 8443
        infra = AWS
        access_key = [...]
        private_key_filename = aws_privatekey.json # pragma: allowlist secret
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
            string accessKey = Config.GetIniValue(ini, profile, "access_key");
            if(accessKey == "notfound")
            {
                throw new KeyNotFoundException("access_key");
            }
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

    }
}
