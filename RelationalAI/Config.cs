using System;
using System.IO;
using System.Runtime.InteropServices;
using IniParser;
using IniParser.Model;

namespace Com.RelationalAI
{
    public class Config
    {
        private static FileIniDataParser parser = new FileIniDataParser();
        /*
            DotRaiConfigPath() -> String

        Returns the path of the config file.

        # Returns
        - `String`
        */
        public static string DotRaiConfigPath()
        {
            return Path.Combine(DotRaiDir(), "config");
        }
        public static string DotRaiDir()
        {
            var envHome = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "HOMEPATH" : "HOME";
            var home = Environment.GetEnvironmentVariable(envHome);
            if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var homeDrive = Environment.GetEnvironmentVariable("HOMEDRIVE");
                home = homeDrive + home;
            }
            return Path.Combine(home, ".rai");
        }


        /*
            LoadDotRaiConfig(path::AbstractString=dotRaiConfigPath()) -> IniData

        Returns the contents of the rAI .ini config file. Currently, this file is assumed to be
        stored at `~/.rai/config`. If no file is found, a `SystemError` is thrown.

        Example `~/.rai/config`:
        ```
        [default]
        region = us-east
        host = azure-dev.relationalai.com
        port = 443
        infra = AZURE
        accessKey = [...]
        privateKeyFilename = default_privatekey.json # pragma: allowlist secret

        [aws]
        region = us-east
        host = 127.0.0.1
        port = 8443
        infra = AWS
        accessKey = [...]
        privateKeyFilename = aws_privatekey.json # pragma: allowlist secret
        ```

        # Arguments
        - `path=dotRaiConfigPath()`: Path to the rAI config file
            (`~/.rai/config` by default)

        # Returns
        - `IniData`: Contents of the config file (.ini format)

        # Throws
        - `SystemError`: If the `path` doesn't exist
        */
        public static IniData LoadDotRaiConfig()
        {
            return LoadDotRaiConfig(DotRaiConfigPath());
        }
        public static IniData LoadDotRaiConfig(string path=null)
        {
            if( path == null ) path = DotRaiConfigPath();
            if (! File.Exists(path)) throw new FileNotFoundException(path + " does not exist");
            return parser.ReadFile(path);
        }

        public static void StoreDotRaiConfig(IniData ini, string path=null)
        {
            if( path == null ) path = DotRaiConfigPath();
            parser.WriteFile(path, ini);
        }

        public static string GetIniValue(
            IniData ini,
            string profile,
            string key,
            string defaultValue="notfound"
        )
        {
            var KeyData = ini[profile].GetKeyData(key);
            return KeyData == null ? defaultValue : KeyData.Value;
        }

        /*
            RaiGetInfra(IniData ini, string profile="default") -> string

        Returns the cloud provider used by the rAI service from the config file, associated with
        the `[profile]`.

        # Arguments
        - `IniData ini`: The contents of the config file (.ini format)
        - `string profile="default"`: The [profile] to look for

        # Returns
        - `string`

        # Throws
        - `KeyError`: If the `profile` doesn't exist
        */
        public static string RaiGetInfra(IniData ini, string profile="default")
        {
            return GetIniValue(ini, profile, "infra", defaultValue:"AWS");
        }


        /*
        RaiGetRegion(IniData ini, string profile="default") -> string

        Returns the region of the rAI service from the config file, associated with the
        `[profile]`.

        # Arguments
        - `IniData ini`: The contents of the config file (.ini format)
        - `string profile="default"`: The [profile] to look for

        # Returns
        - `string`

        # Throws
        - `KeyError`: If the `profile` doesn't exist
        */
        public static string  RaiGetRegion(IniData ini, string profile="default")
        {
            return GetIniValue(ini, profile, "region", defaultValue:"us-east");
        }


        /*
            RaiGetHost(IniData ini, string profile="default") -> String

        Returns the hostname of the rAI service from the config file, associated with the
        `[profile]`.

        # Arguments
        - `IniData ini`: The contents of the config file (.ini format)
        - `string profile="default"`: The [profile] to look for

        # Returns
        - `String`

        # Throws
        - `KeyError`: If the `profile` doesn't exist
        */
        public static string RaiGetHost(IniData ini, string profile="default")
        {
            return GetIniValue(ini, profile, "host", defaultValue:"aws.relationalai.com");
        }


        /*
            RaiGetPort(IniData ini, string profile="default") -> int

        Returns the port of the rAI service from the config file, associated with the `[profile]`.

        # Arguments
        - `IniData ini`: The contents of the config file (.ini format)
        - `string profile="default"`: The [profile] to look for

        # Returns
        - `int`

        # Throws
        - `KeyError`: If the `profile` doesn't exist
        */
        public static int RaiGetPort(IniData ini, string profile="default")
        {
            return int.Parse(GetIniValue(ini, profile, "port", defaultValue:"443"));
        }

        /*
            RaiSetInfra(
                IniData ini,
                string infra,
                string profile="default"
            ) -> void

        Sets the cloud provider used by the rAI service to `infra`, for the specific `[profile]`.

        # Arguments
        - `IniData ini`: The contents of the config file (.ini format)
        - `string infra`: The cloud provider to set
        - `string profile="default"`: The [profile] to look for

        # Returns
        - `void`
        */
        public static void RaiSetInfra(
            IniData ini,
            string infra,
            string profile="default"
        )
        {
            ini[profile]["infra"] = infra;
            return;
        }

        /*
        RaiSetRegion(
                IniData ini,
                string region,
                string profile="default"
            ) -> void

        Sets the region key to `region`, for the specific `[profile]`.

        # Arguments
        - `IniData ini`: The contents of the config file (.ini format)
        - `string region`: The region to set
        - `string profile="default"`: The [profile] to look for

        # Returns
        - `void`
        */
        public static void RaiSetRegion(
            IniData ini,
            string region,
            string profile="default"
        )
        {
            ini[profile]["region"] = region;
            return;
        }

        /*
            RaiSetAccessKey(
                IniData ini,
                string accessKey,
                string profile="default"
            ) -> void

        Sets the access-key key to `access_key`, for the specific `[profile]`.

        # Arguments
        - `IniData ini`: The contents of the config file (.ini format)
        - `string accessKey`: The access-key value to set
        - `string profile="default"`: The [profile] to look for

        # Returns
        - `void`
        */
        public static void RaiSetAccessKey(
            IniData ini,
            string accessKey,
            string profile="default"
        )
        {
            ini[profile]["access_key"] = accessKey;
            return;
        }

        /*
            RaiSetPrivateKeyFilename(
                IniData ini,
                string privateKeyFilename,
                string profile="default"
            ) -> void

        Sets the private-key's filename to `privateKeyFilename`, for the specific `[profile]`.

        # Arguments
        - `IniData ini`: The contents of the config file (.ini format)
        - `string privateKeyFilename`: The private-key filename value to set
        - `string profile="default"`: The [profile] to look for

        # Returns
        - `void`
        */
        public static void RaiSetPrivateKeyFilename(
            IniData ini,
            string privateKeyFilename,
            string profile="default"
        )
        {
            ini[profile]["privateKeyFilename"] = privateKeyFilename;

            return;
        }

    }
}
