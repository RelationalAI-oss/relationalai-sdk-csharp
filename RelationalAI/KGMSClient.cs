using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using RelationalAI.Helpers;

namespace Com.RelationalAI
{
    using AnyValue = System.Object;
    public partial class GeneratedRelationalAIClient
    {
        public Connection conn {get; set;}

        public const string JSON_CONTENT_TYPE = "application/json";
        public const string CSV_CONTENT_TYPE = "text/csv";
        public const string USER_AGENT_HEADER = "KGMSClient/1.2.7/csharp";

        public int DebugLevel = Connection.DEFAULT_DEBUG_LEVEL;

        AsyncLocal<CancellationTokenSource> AsyncLocalKeepAliveCancellationTokenSource =
            new AsyncLocal<CancellationTokenSource>();


        public GeneratedRelationalAIClient(Connection conn)
        {
            this.conn = conn;
            _httpClient = KGMSClient.GetHttpClient(conn.BaseUrl, conn.VerifySSL, conn.ConnectionTimeout);
            _settings = new System.Lazy<Newtonsoft.Json.JsonSerializerSettings>(CreateSerializerSettings);
        }

        partial void PrepareRequest(Transaction body, HttpClient client, HttpRequestMessage request, string url)
        {
            var uriBuilder = new UriBuilder(request.RequestUri);
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);
            if(conn is LocalConnection) {
                query["dbname"] = conn.DbName;
                if(!_isEmpty(body.Source_dbname)) {
                    query["source_dbname"] = body.Source_dbname;
                }
            }
            query["open_mode"] = body.Mode.ToString();
            if(conn is ManagementConnection || conn is CloudConnection) {
                query["region"] = EnumString.GetDescription(conn.Region);
            }
            if(conn is CloudConnection) {
                query["compute_name"] = conn.ComputeName;
                // Note:
                // We need to send the gzip content encoding header and a gzip compressed body only in case of a CloudConnection.
                // Local rai-server cannot handle gzip encoding, only infra server support does.

                // Compress the contents (request body) as gzipped byte array. C# httpclient does not implicitly compress the content over the wire
                // if content encoding is gzip; we need to manually compress the body.
                // Note: If the client sends content-encoding as gzip but does not encode the content to gzip, then the server will return 400 Bad Request.
                request.Content = CompressionUtils.CompressRequestContentAsGzip(request.Content);
                //Set the content encoding type header as gzip. It will tell the server that the content is gzip encoded.
                request.Content.Headers.Add("content-encoding", "gzip");
            }
            uriBuilder.Query = query.ToString();
            request.RequestUri = uriBuilder.Uri;

            // populate headers
            request.Headers.Host = request.RequestUri.Host;
            //Set the content type header
            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json; charset=utf-8");

            // Set Auth here
            var raiRequest = new RAIRequest(request, conn);
            raiRequest.SetAuth();
            KGMSClient.AddExtraHeaders(request);

            // use HTTP 2.0 (to handle keep-alive)
            request.Version =  System.Net.HttpVersion.Version20;
        }

        private bool isStatusRequest(Transaction txn)
        {
            return txn.Actions.Count == 1 && txn.Actions.First().Action is StatusAction;
        }

        partial void ProcessResponse(Transaction body, HttpClient client, HttpResponseMessage response)
        {
            ProcessCleanup(body, client);
        }

        partial void ProcessCleanup(Transaction body, HttpClient client)
        {
            if(!isStatusRequest(body)) {
                var tokenSource = AsyncLocalKeepAliveCancellationTokenSource.Value;
                if(tokenSource != null && !tokenSource.IsCancellationRequested) {
                    tokenSource.Cancel();
                }
            }
        }

        private string BoolStr(bool val) {
            return val ? "true" : "false";
        }

        private bool _isEmpty(string str)
        {
            return str == null || str.Length == 0;
        }
    }

    public partial class Transaction
    {
        public Transaction()
        {
            // You can set the default debug level here to control the debugging information
            // on the server. Currently, any number above 0 results in printing the JSON
            // value for the requests (Transaction) and responses (TransactionResult)
            this.Debug_level = 0;
        }
    }

    partial class RelKey
    {
        private static MultiSetComparer<string> comp = new MultiSetComparer<string>();

        public RelKey()
        {
        }

        public RelKey(string name, List<string> keys = null, List<string> values = null)
        {
            this.Name = name;
            this.Keys = keys == null ? new List<string>() : keys;
            this.Values = values == null ? new List<string>() : values;
        }

        public override bool Equals(object obj)
        {
            return obj is RelKey key &&
                   Name.Equals(key.Name) &&
                   comp.Equals(Keys, key.Keys) &&
                   comp.Equals(Values, key.Values);
        }

        public override int GetHashCode()
        {
            return System.HashCode.Combine(Name, Keys, Values);
        }
    }

    partial class Relation
    {
        public Relation()
        {
        }

        public Relation(RelKey relKey, AnyValue[][] columns) : this(relKey, ToCollection(columns))
        {
        }

        public Relation(RelKey relKey, ICollection<ICollection<AnyValue>> columns) : this()
        {
            this.Rel_key = relKey;
            this.Columns = columns;
        }

        public static AnyValue[][] ToRelData(params AnyValue[] vals) {
            return new AnyValue[][] { vals };
        }

        public static AnyValue[][] ToRelData(params AnyValue[][] vals) {
            return vals;
        }

        public HashSet<HashSet<AnyValue>> ColumnsToHashSet(ICollection<ICollection<AnyValue>> columns)
        {
            return columns.Select(col => col.ToHashSet()).ToHashSet();
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;

            if ( obj is Relation relation ) {
                return EqualityComparer<RelKey>.Default.Equals(Rel_key, relation.Rel_key) &&
                       EqualsToCollection(relation.Columns);
            } else if ( obj.GetType().IsArray ) {
                AnyValue[] arr = (AnyValue[]) obj;
                if( arr.Length == 0 ) {
                    return Columns.Count == 0 || Columns.First().Count == 0;
                } else if ( arr[0].GetType().IsArray ) {
                    return EqualsToArr((dynamic) arr);
                }
            }
            return false;
        }

        public static ICollection<ICollection<AnyValue>> ToCollection(AnyValue[][] arr)
        {
            return arr.Select(col => (ICollection<AnyValue>)(col.Cast<AnyValue>().ToHashSet())).ToHashSet();
        }

        private bool EqualsToArr(AnyValue[][] arr) {
            return EqualsToCollection(ToCollection(arr));
        }

        private bool EqualsToCollection(ICollection<ICollection<AnyValue>> col) {
            var x1 = ColumnsToHashSet(Columns);
            var x2 = ColumnsToHashSet(col);
            return x1.All(elem1 => x2.Any(elem2 => elem1.SetEquals(elem2))) && x2.All(elem2 => x1.Any(elem1 => elem2.SetEquals(elem1)));
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Rel_key, ColumnsToHashSet(Columns));
        }
    }

    public partial class Source
    {
        public Source()
        {
        }
        public Source(string path) : this("", path, "")
        {
        }
        public Source(string name, string value) : this(name, name, value)
        {
        }
        public Source(string name, string path, string value) : this()
        {
            Name = name;
            Path = path;
            Value = value;
        }
    }

    public partial class CSVFileSchema : FileSchema
    {
        public CSVFileSchema(params string[] types)
        {
            this.Types = types.ToList();
        }
    }

    public partial class CSVFileSyntax : FileSyntax
    {
        public CSVFileSyntax(
            ICollection<string> header = null,
            int headerRow = 1,
            bool normalizeNames = false,
            int dataRow = -1,
            ICollection<string> missingStrings = null,
            string delim = ",",
            bool ignoreRepeated = false,
            string quoteChar = "\"",
            string escapeChar = "\\"
        )
        {
            if( missingStrings == null ) missingStrings = new List<string>();

            this.Header = header;
            this.Header_row = headerRow;
            this.Normalizenames = normalizeNames;
            this.Datarow = dataRow;
            this.Missingstrings = missingStrings;
            this.Delim = delim;
            this.Ignorerepeated = ignoreRepeated;
            this.Quotechar = quoteChar;
            this.Escapechar = escapeChar;
        }
    }

    public partial class AzureIntegration : Integration
    {
        public AzureIntegration(
                        string tenant_id = null,
                        string name = null,
                        ICollection<string> storage_allowed_locations = null,
                        ICollection<string> storage_blocked_locations = null,
                        ICollection<Tuple<string, string>> credentials = null
        )
        {

            this.Tenant_id = tenant_id;
            this.Name = name;
            this.Storage_allowed_locations = storage_allowed_locations;
            this.Storage_blocked_locations = storage_blocked_locations;

            var pairs = new List<Pair_Symbol_String_>();

            if (credentials == null) credentials = new List<Tuple<string, string>>();

            foreach (var tuple in credentials)
            {
                var pair = new Pair_Symbol_String_();
                pair.First = tuple.Item1;
                pair.Second = tuple.Item2;
                pairs.Add(pair);
            }

            this.Credentials = pairs;
        }
    }

    public partial class ApiException<TResult> : ApiException
    {
        public override string Message { get {
           return "Result: " + Result.ToString() + "\n\n" + base.Message;
        } }
    }

    public partial class InfraError
    {
        public override string ToString()
        {
            return this.Message;
        }
    }

    public class KGMSClient : GeneratedRelationalAIClient
    {
        public static ILogger DefaultLogger = new ConsoleLogger();
        public static IDictionary<String, String> extraHeaders = new Dictionary<String, String>();

        public ILogger Logger
        {
            get => _logger ?? DefaultLogger;
            set => _logger = value;
        }
        private ILogger _logger;


        public static void AddExtraHeaders(HttpRequestMessage request)
        {
            // host & content-type header for signature verification, more headers here
            request.Headers.UserAgent.TryParseAdd(USER_AGENT_HEADER);
            // add extra headers
            foreach(var item in extraHeaders)
            {
                request.Headers.Add(item.Key, item.Value);
            }
        }

        private static bool httpClientVerifySSL = Connection.DEFAULT_VERIFY_SSL;
        private static HttpClient httpClient = null;

        public string DbName { get { return conn.DbName; } }

        protected internal static HttpClient GetHttpClient(
            Uri url,
            bool verifySSL,
            int connectionTimeout = Connection.DEFAULT_CONNECTION_TIMEOUT
        )
        {
            if( httpClient == null || ("https".Equals(url.Scheme) && httpClientVerifySSL != verifySSL)) {
                // we keep a single static HttpClient instance and keep reusing it instead
                // of creating an instance for each request. This is a proven best practice.
                // However, if we are going to handle a `https` request and suddenly
                // decide a value for `verifySSL` other than its default value (or the value
                // used in the previous requests), then this section disposes the existing
                // HttpClient instance and creates a new one.
                if(httpClient != null) httpClient.Dispose();
                httpClient = HttpClientFactory.CreateHttpClient(verifySSL, connectionTimeout);
                httpClientVerifySSL = verifySSL;

                var sp = ServicePointManager.FindServicePoint(url);
                sp.SetTcpKeepAlive(true, HttpClientFactory.KEEP_ALIVE_TIME*1000, HttpClientFactory.KEEP_ALIVE_INTERVAL*1000);
            }

            return httpClient;
        }

        public KGMSClient(Connection conn) : base(KGMSClient.GetHttpClient(conn.BaseUrl, conn.VerifySSL, conn.ConnectionTimeout))
        {
            this.conn = conn;
            extraHeaders = conn.ExtraHeaders;
            conn.Client = this;
            this.BaseUrl = conn.BaseUrl.ToString();
        }

        public TransactionResult RunTransaction(Transaction xact)
        {
            //TODO: move DebugLevel to ILogger
            if(this.DebugLevel > 0) {
                Logger.Info($"Transaction: {JObject.FromObject(xact)}");
            }
            Task<TransactionResult> responseTask = this.TransactionAsync(xact);
            TransactionResult response = responseTask.Result;

            if(this.DebugLevel > 0) {
                var logMessage = $"TransactionOutput: {JObject.FromObject(response)}";
                if (response.Aborted) Logger.Error(logMessage);
                else if (response.Problems.Any()) Logger.Warning(logMessage);
                else Logger.Info(logMessage);
            }
            return response;
        }

        public ActionResult RunAction(Action action, out bool success, bool isReadOnly = false, TransactionMode mode=TransactionMode.OPEN) {
            return RunAction("single", action, out success, isReadOnly, mode);
        }

        public ActionResult RunAction(Action action, bool isReadOnly = false, TransactionMode mode=TransactionMode.OPEN)
        {
            bool success;
            return RunAction("single", action, out success, isReadOnly, mode);
        }
        public ActionResult RunAction(String name, Action action, out bool success, bool isReadOnly = false, TransactionMode mode=TransactionMode.OPEN)
        {
            this.conn = conn;

            var xact = new Transaction();
            xact.Mode = mode;
            xact.Dbname = conn.DbName;

            var labeledAction = new LabeledAction();
            labeledAction.Name = name;
            labeledAction.Action = action;
            xact.Actions = new List<LabeledAction>();
            xact.Actions.Add(labeledAction);
            xact.Readonly = isReadOnly;

            TransactionResult response = RunTransaction(xact);

            success = IsSuccess(response);
            foreach (LabeledActionResult act in response.Actions)
            {
                if (name.Equals(act.Name))
                {
                    ActionResult res = (ActionResult)act.Result;
                    return res;
                }
            }

            return null;
        }

        private bool IsSuccess(TransactionResult response) {
            return !response.Aborted && response.Problems.Count == 0;
        }

        public bool CloneDatabase(string sourceDbname, bool overwrite = false)
        {
            var xact = new Transaction();
            xact.Mode = overwrite ? TransactionMode.CLONE_OVERWRITE : TransactionMode.CLONE;
            xact.Dbname = conn.DbName;
            xact.Actions = new LinkedList<LabeledAction>();
            xact.Source_dbname = sourceDbname;
            xact.Readonly = false;
            TransactionResult response = RunTransaction(xact);

            if(response.Problems.Count > 0) {
                throw new Exception(response.Problems.ToString());
            }

            return IsSuccess(response);
        }

        public bool CreateDatabase(bool overwrite = false)
        {
            var xact = new Transaction();
            xact.Mode = overwrite ? TransactionMode.CREATE_OVERWRITE : TransactionMode.CREATE;
            xact.Dbname = conn.DbName;
            xact.Actions = new LinkedList<LabeledAction>();
            xact.Readonly = false;

            TransactionResult response = RunTransaction(xact);

            if(response.Problems.Count > 0) {
                throw new Exception(response.Problems.ToString());
            }

            return IsSuccess(response);
        }

        public bool InstallSource(String name, String srcStr)
        {
            return InstallSource(name, name, srcStr);
        }
        public bool InstallSource(String name, String path, String srcStr)
        {
            Source src = new Source();
            src.Name = name;
            src.Path = path;
            src.Value = srcStr;

            return InstallSource(src);
        }
        public bool InstallSource(Source src)
        {
            return InstallSource(new List<Source>() { src });
        }

        public bool InstallSource(ICollection<Source> srcList)
        {
            var action = new InstallAction();
            foreach(Source src in srcList) {
                _readFileFromPath(src);
            }
            action.Sources = srcList;

            return RunAction(action) != null;
        }

        private void _readFileFromPath(Source src)
        {
            if(!_isEmpty(src.Path)) {
                if(_isEmpty(src.Name)) {
                    src.Name = Path.GetFileNameWithoutExtension(src.Path);
                }
                if(_isEmpty(src.Value) && File.Exists(src.Path)) {
                    src.Value = File.ReadAllText(src.Path);
                }
            }
        }

        public bool DeleteSource(ICollection<string> srcNameList)
        {
            var action = new ModifyWorkspaceAction();
            action.Delete_source = srcNameList;

            bool success;
            var actionRes = (ModifyWorkspaceActionResult)RunAction(action, out success);
            return success && actionRes != null;
        }

        public bool DeleteSource(string srcName)
        {
            return DeleteSource(new List<string>() { srcName });
        }

        public IDictionary<string, Source> ListSource()
        {
            var action = new ListSourceAction();
            var actionRes = (ListSourceActionResult)RunAction(action, isReadOnly: true);

            var resultDict = new Dictionary<string, Source>();
            foreach(Source src in actionRes.Sources) {
                resultDict[src.Name] = src;
            }

            return resultDict;
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
            return Query(name, path, srcStr, inputs, new List<string>() { output }, persist, isReadOnly, mode);
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
            if(path == null) path = name;

            var src = new Source();
            src.Name = name;
            src.Path = path;
            src.Value = srcStr;

            return _filterDictionary(Query(src, inputs, outputs, persist, isReadOnly, mode), outputs);
        }

        private IDictionary<RelKey, Relation> _filterDictionary(IDictionary<RelKey, Relation> dict, ICollection<string> outputs)
        {
            if(dict.All(kvp => outputs.Contains(kvp.Key.Name))) return dict;
            return dict.Where(kvp => outputs.Contains(kvp.Key.Name))
                       .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
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
            if(src == null) {
                src = new Source();
                src.Name = "";
                src.Path = "";
                src.Value = "";
            }
            if(inputs == null) inputs = new List<Relation>();
            if(outputs == null) outputs = new List<string>();
            if(persist == null) persist = new List<string>();


            var action = new QueryAction();
            action.Inputs = inputs;
            action.Source = src;
            action.Outputs = outputs;
            action.Persist = persist;

            var actionRes = (QueryActionResult)RunAction(
                action,
                isReadOnly.GetValueOrDefault(persist.Count == 0),
                mode.GetValueOrDefault(conn.DefaultOpenMode)
            );

            return actionRes == null ? new Dictionary<RelKey, Relation>() : _convertToDictionary(actionRes.Output);
        }

        private IDictionary<RelKey, Relation> _convertToDictionary(ICollection<Relation> output)
        {
            var outDict = new Dictionary<RelKey, Relation>();
            if(output != null) {
                foreach(Relation rel in output) {
                    outDict[rel.Rel_key] = rel;
                }
            }
            return outDict;
        }

        private static ICollection<Pair_AnyValue_AnyValue_> _convertCollection(ICollection<Tuple<AnyValue, AnyValue>> data)
        {
            var res = new List<Pair_AnyValue_AnyValue_>();
            foreach(var tpl in data) {
                var pair = new Pair_AnyValue_AnyValue_();
                pair.First = tpl.Item1;
                pair.Second = tpl.Item2;
                res.Add(pair);
            }
            return res;
        }

        public bool UpdateEdb(
            RelKey rel,
            ICollection<Tuple<AnyValue, AnyValue>> updates = null,
            ICollection<Tuple<AnyValue, AnyValue>> delta = null
        )
        {
            var action = new UpdateAction();
            action.Rel = rel;
            if(updates != null) action.Updates = _convertCollection(updates);
            if(delta != null)action.Delta = _convertCollection(delta);

            return RunAction(action, isReadOnly: false) != null;
        }

        private void _handleNullFieldsForLoadData(LoadData loadData)
        {
            if(!_isEmpty(loadData.Path) && !_isEmpty(loadData.Data)) {
                var message = string.Format(
                    "Either `Path` or `Data` should be specified for `LoadData`." +
                    "You have provided both: `Path`={0} and `Data`={1}",
                    loadData.Path,
                    loadData.Data
                );
                throw new ArgumentException(message);
            }

            if(_isEmpty(loadData.Path) && _isEmpty(loadData.Data)) {
                var message = "Either `Path` or `Data` is required.";
                throw new ArgumentException(message);
            }

            if(_isEmpty(loadData.Content_type)) {
                throw new ArgumentException("`ContentType` is required.");
            }

            if(!loadData.Content_type.StartsWith(JSON_CONTENT_TYPE) && !loadData.Content_type.StartsWith(CSV_CONTENT_TYPE)) {
                throw new ArgumentException(string.Format("`ContentType`={0} is not supported.", loadData.Content_type));
            }
        }
        private bool _isEmpty(string str)
        {
            return str == null || str.Length == 0;
        }
        private void _readFileFromPath(LoadData loadData)
        {
            if(!_isEmpty(loadData.Path) && _isEmpty(loadData.Data)) {
                if(File.Exists(loadData.Path)) {
                    loadData.Data = File.ReadAllText(loadData.Path);
                    loadData.Path = null;
                }
            }
        }

        public LoadData JsonString(
            string data,
            AnyValue key = null,
            FileSyntax syntax = null,
            FileSchema schema = null
        )
        {
            var loadData = new LoadData();
            loadData.Data = data;
            loadData.Content_type = JSON_CONTENT_TYPE;
            loadData.Key = key;
            loadData.File_syntax = syntax;
            loadData.File_schema = schema;

            return loadData;
        }

        public LoadData JsonFile(
            string filePath,
            AnyValue key = null,
            FileSyntax syntax = null,
            FileSchema schema = null
        )
        {
            var loadData = new LoadData();
            loadData.Path = filePath;
            loadData.Content_type = JSON_CONTENT_TYPE;
            loadData.Key = key;
            loadData.File_syntax = syntax;
            loadData.File_schema = schema;

            return loadData;
        }

        public LoadData CsvString(
            string data,
            AnyValue key = null,
            FileSyntax syntax = null,
            FileSchema schema = null
        )
        {
            var loadData = new LoadData();
            loadData.Data = data;
            loadData.Content_type = CSV_CONTENT_TYPE;
            loadData.Key = key;
            loadData.File_syntax = syntax;
            loadData.File_schema = schema;

            return loadData;
        }

        public LoadData CsvFile(
            string filePath,
            AnyValue key = null,
            FileSyntax syntax = null,
            FileSchema schema = null,
            Integration integration = null
        )
        {
            var loadData = new LoadData();
            loadData.Path = filePath;
            loadData.Content_type = CSV_CONTENT_TYPE;
            loadData.Key = key;
            loadData.File_syntax = syntax;
            loadData.File_schema = schema;
            loadData.Integration = integration;

            return loadData;
        }

        public bool LoadEdb(
            string rel,
            string contentType,
            string data = null,
            string path = null,
            AnyValue key = null,
            FileSyntax syntax = null,
            FileSchema schema = null,
            Integration integration = null
        )
        {
            if(key == null) key = new int[] {};

            var loadData = new LoadData();
            loadData.Content_type = contentType;
            loadData.Data = data;
            loadData.Path = path;
            loadData.Key = key;
            loadData.File_syntax = syntax;
            loadData.File_schema = schema;
            loadData.Integration = integration;

            return LoadEdb(rel, loadData);
        }
        public bool LoadEdb(string rel, LoadData value)
        {
            _handleNullFieldsForLoadData(value);
            _readFileFromPath(value);
            var action = new LoadDataAction();
            action.Rel = rel;
            action.Value = value;

            return RunAction(action, isReadOnly: false) != null;
        }

        private static string _typeToString(Type tp)
        {
            var str = tp.ToString();
            return tp.Name;
        }

        public bool LoadEdb(string relName, AnyValue[][] columns)
        {
            return LoadEdb(relName, Relation.ToCollection(columns));
        }

        public bool LoadEdb(string relName, ICollection<ICollection<AnyValue>> columns)
        {
            var rel = new Relation();
            rel.Rel_key = new RelKey(relName);
            if( columns != null && columns.Count > 0 && columns.First().Count > 0) {
                Debug.Assert(columns.All(col => col.Count == columns.First().Count));
                foreach(var col in columns) {
                    rel.Rel_key.Keys.Add(_typeToString(col.First().GetType()));
                }
            }

            rel.Columns = columns;
            return LoadEdb(rel);
        }

        public bool LoadEdb(RelKey relKey, ICollection<ICollection<AnyValue>> columns)
        {
            var rel = new Relation();
            rel.Rel_key = relKey;
            rel.Columns = columns;
            return LoadEdb(rel);
        }
        public bool LoadEdb(Relation value)
        {
            return LoadEdb( new List<Relation>() { value } );
        }
        public bool LoadEdb(ICollection<Relation> value)
        {
            var action = new ImportAction();
            action.Inputs = value;

            return RunAction(action, isReadOnly: false) != null;
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
            return LoadEdb(rel, CSV_CONTENT_TYPE, data, path, key, syntax, schema, integration);
        }

        public bool LoadJSON(
            string rel,
            string data = null,
            string path = null,
            AnyValue[] key = null,
            Integration integration = null
        )
        {
            return LoadEdb(rel, JSON_CONTENT_TYPE, data, path, key, new JSONFileSyntax(), new JSONFileSchema(), integration);
        }

        public ICollection<RelKey> ListEdb(string relName = null)
        {
            var action = new ListEdbAction();
            if(relName != null) action.Relname = relName;
            var actionRes = (ListEdbActionResult)RunAction(action, isReadOnly: true);
            return actionRes.Rels;
        }

        public ICollection<RelKey> DeleteEdb(string relName)
        {
            var action = new ModifyWorkspaceAction();
            action.Delete_edb = relName;
            var actionRes = (ModifyWorkspaceActionResult)RunAction(action);
            return actionRes.Delete_edb_result;
        }

        public bool EnableLibrary(string srcName)
        {
            var action = new ModifyWorkspaceAction();
            action.Enable_library = srcName;
            return RunAction(action) != null;
        }

        public bool CloseDatabase()
        {
            var action = new ModifyWorkspaceAction();
            action.Close_database = conn.DbName;
            return RunAction(action, isReadOnly: true) != null;
        }

        public ICollection<Relation> Cardinality(string relName = null)
        {
            var action = new CardinalityAction();
            if(relName != null) action.Relname = relName;
            return ((CardinalityActionResult)RunAction(action, isReadOnly: true)).Result;
        }

        public ICollection<AbstractProblem> CollectProblems()
        {
            var action = new CollectProblemsAction();
            var actionRes = (CollectProblemsActionResult)RunAction(action, isReadOnly: true);
            return actionRes.Problems;
        }

        public bool Configure(
            bool? debug = null,
            bool? debugTrace = null,
            bool? silent = null,
            bool? abortOnError = null
        )
        {
            var action = new SetOptionsAction();
            action.Debug = debug;
            action.Debug_trace = debugTrace;
            action.Silent = silent;
            action.Abort_on_error = abortOnError;
            return RunAction(action, isReadOnly: false) != null;
        }

        public bool Status()
        {
            var action = new StatusAction();
            return RunAction(action, isReadOnly: true) != null;
        }
    }
}
