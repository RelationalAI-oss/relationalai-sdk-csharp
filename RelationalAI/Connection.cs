using System;
using System.Collections.Generic;
using System.Linq;

namespace Com.RelationalAI
{
    using AnyValue = System.Object;

    public abstract class Connection
    {
        public const string DEFAULT_SCHEME = "http";
        public const string DEFAULT_HOST = "127.0.0.1";
        public const int DEFAULT_PORT = 8010;
        public const TransactionMode DEFAULT_OPEN_MODE = TransactionMode.OPEN;
        public const RAIInfra DEFAULT_INFRA = RAIInfra.AZURE;
        public const RAIRegion DEFAULT_REGION = RAIRegion.US_EAST;
        public const bool DEFAULT_VERIFY_SSL = true;
        public const int DEFAULT_DEBUG_LEVEL = 0;
        public const int DEFAULT_CONNECTION_TIMEOUT = 300; // seconds

        public virtual string DbName => throw new InvalidOperationException();

        public virtual TransactionMode DefaultOpenMode => throw new InvalidOperationException();

        public virtual string Scheme => throw new InvalidOperationException();

        public virtual string Host => throw new InvalidOperationException();

        public virtual int Port => throw new InvalidOperationException();

        public virtual RAIInfra Infra => throw new InvalidOperationException();

        public virtual RAIRegion Region => throw new InvalidOperationException();

        public virtual RAICredentials Creds => throw new InvalidOperationException();

        public virtual bool VerifySSL => throw new InvalidOperationException();

        public virtual string ComputeName => throw new InvalidOperationException();

        public virtual int Version { get { throw new InvalidOperationException(); } set { throw new InvalidOperationException(); } }

        public Uri BaseUrl {
            get { return new UriBuilder(this.Scheme, this.Host, this.Port).Uri; }
        }

        public int ConnectionTimeout { get; set; }

        public KGMSClient Client { get; set; }
        public ManagementClient CloudClient { get; set; }

        public void SetConnectionOnClients() {
            if(Client != null) Client.conn = this;
            if(CloudClient != null) CloudClient.conn = this;
        }

        public int DebugLevel {
            get{ return Client != null ? Client.DebugLevel : DEFAULT_DEBUG_LEVEL; }
            set { if(Client != null) Client.DebugLevel = value; }
        }
    }

    /// <summary>
    /// Connection working with databases in a locally running rai-server.
    /// </summary>
    public class LocalConnection : Connection
    {
        /// <summary>
        /// Connection working with databases in a locally running rai-server.
        /// </summary>
        /// <param name="dbname">database to execute transactions with</param>
        /// <param name="defaultOpenMode">`= TransactionMode.OPEN`: How to open the database `dbname`</param>
        /// <param name="scheme">= `http`: The scheme used for connecting to a running server (e.g., `http`)</param>
        /// <param name="host"> = `127.0.0.1`: The host of a running server.</param>
        /// <param name="port"> = `8010`: The port of a running server.</param>
        public LocalConnection(
            string dbname,
            TransactionMode defaultOpenMode = DEFAULT_OPEN_MODE,
            string scheme = DEFAULT_SCHEME,
            string host = DEFAULT_HOST,
            int port = DEFAULT_PORT,
            int connectionTimeout = DEFAULT_CONNECTION_TIMEOUT
        )
        {
            this.DbName = dbname;
            this.DefaultOpenMode = defaultOpenMode;
            this.Scheme = scheme;
            this.Host = host;
            this.Port = port;
            this.ConnectionTimeout = connectionTimeout;

            if(this.GetType() == typeof(LocalConnection))
            {
                new KGMSClient(this); //to register the connection with a client
            }
            else
            {
                // If it's a subtype of `LocalConnection`, then its association to a `KGMSClient`
                // is done separately in the leaf class.
            }
        }

        public override string DbName { get; }
        public override TransactionMode DefaultOpenMode { get; }
        public override string Scheme { get; }
        public override string Host { get; }
        public override int Port { get; }
        public override bool VerifySSL => DEFAULT_VERIFY_SSL;

        public override int Version { get; set; }

        public bool CloneDatabase(string sourceDbname, bool overwrite=false)
        {
            SetConnectionOnClients();
            return Client.CloneDatabase(sourceDbname, overwrite);
        }

        public bool CloneDatabase(LocalConnection conn, bool overwrite=false)
        {
            return CloneDatabase(conn.DbName, overwrite);
        }

        public bool CreateDatabase(bool overwrite = false)
        {
            SetConnectionOnClients();
            return Client.CreateDatabase(overwrite);
        }

        public bool InstallSource(Source src)
        {
            SetConnectionOnClients();
            return Client.InstallSource(src);
        }

        public bool InstallSource(String name, String srcStr)
        {
            SetConnectionOnClients();
            return Client.InstallSource(name, srcStr);
        }

        public bool InstallSource(String name, String path, String srcStr)
        {
            SetConnectionOnClients();
            return Client.InstallSource(name, path, srcStr);
        }

        public bool InstallSource(ICollection<Source> srcList)
        {
            SetConnectionOnClients();
            return Client.InstallSource(srcList);
        }

        public bool DeleteSource(ICollection<string> srcNameList)
        {
            SetConnectionOnClients();
            return Client.DeleteSource(srcNameList);
        }

        public bool DeleteSource(string srcName)
        {
            SetConnectionOnClients();
            return Client.DeleteSource(srcName);
        }

        public IDictionary<string, Source> ListSource()
        {
            SetConnectionOnClients();
            return Client.ListSource();
        }

        public IDictionary<RelKey, Relation> Query(
            string output,
            string name = "query",
            string path = null,
            string srcStr = "",
            ICollection<Relation> inputs = null,
            ICollection<string> persist = null,
            bool? isReadOnly = null,
            TransactionMode? mode = null
        )
        {
            SetConnectionOnClients();
            return Client.Query(output, name, path, srcStr, inputs, persist, isReadOnly, mode);
        }

        public IDictionary<RelKey, Relation> Query(
            string name = "query",
            string path = null,
            string srcStr = "",
            ICollection<Relation> inputs = null,
            ICollection<string> outputs = null,
            ICollection<string> persist = null,
            bool? isReadOnly = null,
            TransactionMode? mode = null
        )
        {
            SetConnectionOnClients();
            return Client.Query(name, path, srcStr, inputs, outputs, persist, isReadOnly, mode);
        }

        public IDictionary<RelKey, Relation> Query(
            Source src = null,
            ICollection<Relation> inputs = null,
            ICollection<string> outputs = null,
            ICollection<string> persist = null,
            bool? isReadOnly = null,
            TransactionMode? mode = null
        )
        {
            SetConnectionOnClients();
            return Client.Query(src, inputs, outputs, persist, isReadOnly, mode);
        }

        public bool UpdateEdb(
            RelKey rel,
            ICollection<Tuple<AnyValue, AnyValue>> updates = null,
            ICollection<Tuple<AnyValue, AnyValue>> delta = null
        )
        {
            SetConnectionOnClients();
            return Client.UpdateEdb(rel, updates, delta);
        }

        public LoadData JsonString(
            string data,
            AnyValue key = null,
            FileSyntax syntax = null,
            FileSchema schema = null
        )
        {
            SetConnectionOnClients();
            return Client.JsonString(data, key, syntax, schema);
        }

        public LoadData JsonFile(
            string filePath,
            AnyValue key = null,
            FileSyntax syntax = null,
            FileSchema schema = null
        )
        {
            SetConnectionOnClients();
            return Client.JsonString(filePath, key, syntax, schema);
        }

        public LoadData CsvString(
            string data,
            AnyValue key = null,
            FileSyntax syntax = null,
            FileSchema schema = null
        )
        {
            SetConnectionOnClients();
            return Client.JsonString(data, key, syntax, schema);
        }

        public LoadData CsvFile(
            string filePath,
            AnyValue key = null,
            FileSyntax syntax = null,
            FileSchema schema = null
        )
        {
            SetConnectionOnClients();
            return Client.CsvFile(filePath, key, syntax, schema);
        }

        public bool LoadEdb(
            string rel,
            string contentType,
            string data = null,
            string path = null,
            AnyValue key = null,
            FileSyntax syntax = null,
            FileSchema schema = null
        )
        {
            SetConnectionOnClients();
            return Client.LoadEdb(rel, contentType, data, path, key, syntax, schema);
        }

        public bool LoadEdb(string rel, LoadData value)
        {
            SetConnectionOnClients();
            return Client.LoadEdb(rel, value);
        }

        public bool LoadEdb(string relName, AnyValue[][] columns)
        {
            SetConnectionOnClients();
            return Client.LoadEdb(relName, columns);
        }

        public bool LoadEdb(string relName, ICollection<ICollection<AnyValue>> columns)
        {
            SetConnectionOnClients();
            return Client.LoadEdb(relName, columns);
        }

        public bool LoadEdb(RelKey relKey, ICollection<ICollection<AnyValue>> columns)
        {
            SetConnectionOnClients();
            return Client.LoadEdb(relKey, columns);
        }

        public bool LoadEdb(Relation value)
        {
            SetConnectionOnClients();
            return Client.LoadEdb(value);
        }

        public bool LoadEdb(ICollection<Relation> value)
        {
            SetConnectionOnClients();
            return Client.LoadEdb(value);
        }

        public bool LoadCSV(
            string rel,
            string data = null,
            string path = null,
            AnyValue[] key = null,
            FileSyntax syntax = null,
            FileSchema schema = null,
            Integration integration = null
        )
        {
            SetConnectionOnClients();
            return Client.LoadCSV(rel, data, path, key, syntax, schema, integration);
        }

        public bool LoadJSON(
            string rel,
            string data = null,
            string path = null,
            AnyValue[] key = null,
            Integration integration = null
        )
        {
            SetConnectionOnClients();
            return Client.LoadJSON(rel, data, path, key, integration);
        }

        public ICollection<RelKey> ListEdb(string relName = null)
        {
            SetConnectionOnClients();
            return Client.ListEdb(relName);
        }

        public ICollection<RelKey> DeleteEdb(string relName)
        {
            SetConnectionOnClients();
            return Client.DeleteEdb(relName);
        }

        public bool EnableLibrary(string srcName)
        {
            SetConnectionOnClients();
            return Client.EnableLibrary(srcName);
        }

        public ICollection<Relation> Cardinality(string relName = null)
        {
            SetConnectionOnClients();
            return Client.Cardinality(relName);
        }

        public ICollection<AbstractProblem> CollectProblems()
        {
            SetConnectionOnClients();
            return Client.CollectProblems();
        }

        public bool Configure(
            bool? debug = null,
            bool? debugTrace = null,
            bool? silent = null,
            bool? abortOnError = null
        )
        {
            SetConnectionOnClients();
            return Client.Configure(debug, debugTrace, silent, abortOnError);
        }

        public bool Status()
        {
            SetConnectionOnClients();
            return Client.Status();
        }
    }

    public class ManagementConnection : Connection
    {
        public ManagementConnection(
            string configPath,
            string profile = "default",
            string scheme = Connection.DEFAULT_SCHEME,
            string host = Connection.DEFAULT_HOST,
            int port = Connection.DEFAULT_PORT,
            RAIInfra infra = Connection.DEFAULT_INFRA,
            RAIRegion region = Connection.DEFAULT_REGION,
            bool verifySSL = Connection.DEFAULT_VERIFY_SSL,
            int connectionTimeout = Connection.DEFAULT_CONNECTION_TIMEOUT
        ) : this(scheme, host, port, infra, region, _read_creds(configPath, profile), verifySSL, connectionTimeout)
        {
        }

        public ManagementConnection(
            string scheme = Connection.DEFAULT_SCHEME,
            string host = Connection.DEFAULT_HOST,
            int port = Connection.DEFAULT_PORT,
            RAIInfra infra = Connection.DEFAULT_INFRA,
            RAIRegion region = Connection.DEFAULT_REGION,
            RAICredentials creds = null,
            bool verifySSL = Connection.DEFAULT_VERIFY_SSL,
            int connectionTimeout = Connection.DEFAULT_CONNECTION_TIMEOUT
        )
        {
            Scheme = scheme;
            Host = host;
            Port = port;
            Infra = infra;
            Region = region;
            Creds = creds;
            VerifySSL = verifySSL;
            ConnectionTimeout = connectionTimeout;

            if(creds == null) {
                this.Creds = _read_creds(Config.DotRaiConfigPath());
            }

            new ManagementClient(this); //to register the connection with a client
        }

        public static RAICredentials _read_creds(string configPath, string profile="default")
        {
            try {
                // Try to load the credentials.
                return RAICredentials.FromFile(configPath, profile);
            } catch (Exception e) {
                // No credentials found. It's ok. Let's just warn the user.
                Console.WriteLine(string.Format("[WARN] Credential File ({0}) Not Found!"), Config.DotRaiConfigPath());
            }
            return null;
        }

        public override string Scheme { get; }
        public override string Host { get; }
        public override int Port { get; }
        public override RAIInfra Infra { get; }
        public override RAIRegion Region { get; }
        public override RAICredentials Creds { get; }
        public override bool VerifySSL { get; }

        public ICollection<ComputeInfoProtocol> ListComputes(RAIComputeFilters filters = null)
        {
            SetConnectionOnClients();
            return CloudClient.ListComputes(filters);
        }

        public ComputeInfoProtocol CreateCompute(string computeName, RAIComputeSize size = RAIComputeSize.XS, string region = null)
        {
            SetConnectionOnClients();
            return CloudClient.CreateCompute(computeName, size, region);
        }

        public void DeleteCompute(string computeName)
        {
            SetConnectionOnClients();
            CloudClient.DeleteCompute(computeName);
            return;
        }

        public ICollection<UserInfoProtocol> ListUsers()
        {
            SetConnectionOnClients();
            return CloudClient.ListUsers();
        }

        public Tuple<UserInfoProtocol, string> CreateUser(string username)
        {
            SetConnectionOnClients();
            return CloudClient.CreateUser(username);
        }

        public ICollection<DatabaseInfo> ListDatabases(RAIDatabaseFilters filters = null)
        {
            SetConnectionOnClients();
            return CloudClient.ListDatabases(filters);
        }

        public void RemoveDefaultCompute(string dbname)
        {
            SetConnectionOnClients();
            CloudClient.RemoveDefaultCompute(dbname);
        }

        public ICollection<ComputeEventInfo> ListComputeEvents(string computeId)
        {
            SetConnectionOnClients();
            return CloudClient.ListComputeEvents(computeId);
        }

        public GetAccountCreditsResponse GetAccountCreditUsage(Period period=Period.Current_month)
        {
            SetConnectionOnClients();
            return CloudClient.GetAccountCreditUsage(period);
        }
    }

    /// <summary>
    /// Connection for working with databases in the rAI Cloud.
    ///
    /// All details required for communicating with the rAI Cloud frontend are offloaded to the
    /// `management_conn`, i.e. `management_conn` knows where and how to connect/authenticate.
    ///
    /// Executing a transaction on the rAI Cloud requires a compute. A compute is where the actual
    /// database processing is taking place. Each database operation has to be directed to a
    /// compute, either implicitly or explicitly. When a compute is specified in `CloudConnection`,
    /// it will be used for all transactions using this connection. Otherwise, the default compute
    /// will be picked to fulfill the transaction. A default compute can be set through
    /// `createDatabase` (implicitly) or through `setDefaultCompute` (explicitly).
    /// </summary>
    public class CloudConnection : LocalConnection
    {
        /// <summary>
        /// CloudConnection constructor
        /// </summary>
        /// <param name="conn">The base connection to use `LocalConnection` parameters from.</param>
        /// <param name="infra">Underlying cloud provider (AWS/AZURE)</param>
        /// <param name="region">Region of rAI Cloud deployments</param>
        /// <param name="creds">Credentials for authenticating with rAI Cloud</param>
        /// <param name="verifySSL">Verify SSL configuration</param>
        /// <param name="computeName">Compute to be used for this connection. If not specified, the default compute will be used.</param>
        public CloudConnection(
            Connection conn,
            RAIInfra infra = Connection.DEFAULT_INFRA,
            RAIRegion region = Connection.DEFAULT_REGION,
            RAICredentials creds = null,
            bool verifySSL = Connection.DEFAULT_VERIFY_SSL,
            string computeName = null,
            int connectionTimeout = Connection.DEFAULT_CONNECTION_TIMEOUT
        ) : this(conn.DbName, conn.DefaultOpenMode, conn.Scheme, conn.Host, conn.Port, infra, region, creds, verifySSL, computeName, connectionTimeout)
        {
        }

        /// <summary>
        /// CloudConnection constructor
        /// </summary>
        /// <param name="dbname">database to execute transactions with</param>
        /// <param name="defaultOpenMode">`= TransactionMode.OPEN`: How to open the database `dbname`</param>
        /// <param name="scheme">= `http`: The scheme used for connecting to a running server (e.g., `http`)</param>
        /// <param name="host"> = `127.0.0.1`: The host of a running server.</param>
        /// <param name="port"> = `8010`: The port of a running server.</param>
        /// <param name="infra">Underlying cloud provider (AWS/AZURE)</param>
        /// <param name="region">Region of rAI Cloud deployments</param>
        /// <param name="creds">Credentials for authenticating with rAI Cloud</param>
        /// <param name="verifySSL">Verify SSL configuration</param>
        /// <param name="computeName">Compute to be used for this connection. If not specified, the default compute will be used.</param>
        public CloudConnection(
            string dbname,
            TransactionMode defaultOpenMode = Connection.DEFAULT_OPEN_MODE,
            string scheme = Connection.DEFAULT_SCHEME,
            string host = Connection.DEFAULT_HOST,
            int port = Connection.DEFAULT_PORT,
            RAIInfra infra = Connection.DEFAULT_INFRA,
            RAIRegion region = Connection.DEFAULT_REGION,
            RAICredentials creds = null,
            bool verifySSL = Connection.DEFAULT_VERIFY_SSL,
            string computeName = null,
            int connectionTimeout = Connection.DEFAULT_CONNECTION_TIMEOUT
        ) : base(dbname, defaultOpenMode, scheme, host, port, connectionTimeout)
        {
            this.managementConn = new ManagementConnection(scheme, host, port, infra, region, creds, verifySSL, connectionTimeout);
            this.ComputeName = computeName;

            new KGMSClient(this); //to register the connection with a client
        }

        /// <summary>
        /// CloudConnection constructor
        /// </summary>
        /// <param name="dbname">database to execute transactions with</param>
        /// <param name="managementConn">the management connection instance</param>
        /// <param name="defaultOpenMode">`= TransactionMode.OPEN`: How to open the database `dbname`</param>
        /// <param name="computeName">Compute to be used for this connection. If not specified, the default compute will be used.</param>
        public CloudConnection(
            string dbname,
            ManagementConnection managementConn,
            TransactionMode defaultOpenMode = Connection.DEFAULT_OPEN_MODE,
            string computeName = null,
            int connectionTimeout = Connection.DEFAULT_CONNECTION_TIMEOUT
        ) : base(dbname, defaultOpenMode, managementConn.Scheme, managementConn.Host, managementConn.Port, connectionTimeout)
        {
            this.managementConn = managementConn;
            this.ComputeName = computeName;

            new KGMSClient(this); //to register the connection with a client
        }

        private ManagementConnection managementConn { get; }
        public override RAIInfra Infra { get { return managementConn.Infra; } }
        public override RAIRegion Region { get { return managementConn.Region; } }
        public override RAICredentials Creds { get { return managementConn.Creds; } }
        public override bool VerifySSL { get { return managementConn.VerifySSL; } }
        public override string ComputeName { get; }
    }
}
