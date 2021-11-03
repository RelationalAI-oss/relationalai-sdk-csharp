using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Com.RelationalAI
{

    public enum RAIComputeSize
    {
        XS, S, M, L, XL
    }

    public class RAIComputeFilters
    {
        public List<string> Id {get;}
        public List<string> Name {get;}
        public List<RAIComputeSize> Size {get;}
        public List<string> State {get;}

        public RAIComputeFilters(
            List<string> id = null,
            List<string> name = null,
            List<RAIComputeSize> size = null,
            List<string> state = null
        )
        {
            this.Id = id;
            this.Name = name;
            this.Size = size;
            this.State = state;
        }
    }

    public class RAIDatabaseFilters
    {
        public List<string> Id {get;}
        public List<string> Name {get;}
        public List<string> State {get;}

        public RAIDatabaseFilters(
            List<string> id = null,
            List<string> name = null,
            List<string> state = null
        )
        {
            this.Id = id;
            this.Name = name;
            this.State = state;
        }
    }


    public partial class GeneratedRAICloudClient
    {
        public Connection conn {get; set;}

        partial void PrepareRequest(System.Net.Http.HttpClient client, System.Net.Http.HttpRequestMessage request, string url)
        {
            if (request.Content == null)
                request.Content = new StringContent("");
            // populate headers
            request.Headers.Clear();
            request.Content.Headers.Clear();
            request.Headers.Host = request.RequestUri.Host;
            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

            RAIRequest raiReq = new RAIRequest(request, conn);
            raiReq.SetAuth();
            KGMSClient.AddExtraHeaders(request);
        }
    }

    public class ManagementClient : GeneratedRAICloudClient
    {
        public ManagementClient(HttpClient client) : base(client)
        {
            // This constructor is only here to avoid touching the generated code.
            // THIS CONSTRUCTOR SHOULD NOT BE USED.
            throw new InvalidOperationException();
        }

        public ManagementClient(Connection conn) : base(KGMSClient.GetHttpClient(conn.BaseUrl, conn.VerifySSL, conn.ConnectionTimeout))
        {
            this.conn = conn;
            this.conn.CloudClient = this;
            this.BaseUrl = conn.BaseUrl.ToString();
            System.AppDomain.CurrentDomain.UnhandledException += GlobalExceptionHandler;
        }

        public ICollection<ComputeInfoProtocol> ListComputes(RAIComputeFilters filters = null)
        {
            ListComputesResponseProtocol res;
            if (filters == null)
            {
                res = this.ComputeGetAsync(null, null, null, null).Result;
            }
            else
            {
                IEnumerable<string> sizeFilters = null;
                if (filters.Size != null)
                {
                    sizeFilters = filters.Size.Select(s => s.GetDescription());
                }

                res = this.ComputeGetAsync(filters.Id, filters.Name, sizeFilters, filters.State).Result;
            }
            if ( res == null ) return null;

            return res.Computes;
        }

        public ComputeInfoProtocol CreateCompute(string name, RAIComputeSize size = RAIComputeSize.XS, string region = null, bool dryRun = false)
        {
            if(region == null) region = EnumString.GetDescription(this.conn.Region);

            CreateComputeRequestProtocol request = new CreateComputeRequestProtocol();
            request.Region = region;
            request.Name = name;
            request.Size = EnumString.GetDescription(size);
            request.Dryrun = dryRun;
            return this.ComputePutAsync(request).Result.Compute;
        }

        public DeleteComputeStatus DeleteCompute(string computeName, bool dryRun = false)
        {
            DeleteComputeRequestProtocol request = new DeleteComputeRequestProtocol();
            request.Name = computeName;
            request.Dryrun = dryRun;
            return this.ComputeDeleteAsync(request).Result.Status;
        }

        public ICollection<DatabaseInfo> ListDatabases(RAIDatabaseFilters filters = null) {
            ListDatabasesResponseProtocol res;
            if (filters == null)
            {
                res = this.DatabaseGetAsync(null, null, null).Result;
            }
            else
            {
                res = this.DatabaseGetAsync(filters.Id, filters.Name, filters.State).Result;
            }
            if ( res == null ) return null;

            return res.Databases;
        }

        public void RemoveDefaultCompute(string dbname)
        {
            this.UpdateDatabase(dbname, null, removeDefaultCompute: true, dryRun: false);
        }

        public void UpdateDatabase(string name, string defaultComputeName, bool removeDefaultCompute, bool dryRun = false)
        {
            UpdateDatabaseRequestProtocol request = new UpdateDatabaseRequestProtocol();
            request.Name = name;
            request.Default_compute_name = defaultComputeName;
            request.Remove_default_compute = removeDefaultCompute;
            request.Dryrun = dryRun;
            this.DatabasePostAsync(request).Wait();
        }
        public ICollection<UserInfoProtocol> ListUsers()
        {
            return this.UserGetAsync().Result.Users;
        }

        public Tuple<UserInfoProtocol, string> CreateUser(string username, bool dryRun = false)
        {
            CreateUserRequestProtocol request = new CreateUserRequestProtocol();
            request.Username = username;
            request.Dryrun = dryRun;
            var res = this.UserPutAsync(request).Result;
            return new Tuple<UserInfoProtocol, string>(res.User, res.Private_key);
        }

        public ICollection<ComputeEventInfo> ListComputeEvents(string computeId)
        {
            return this.ListComputeEventsAsync(computeId).Result.Events;
        }

        public GetAccountCreditsResponse GetAccountCreditUsage(Period period=Period.Current_month)
        {
            return this.AccountCreditsGetAsync(period).Result;
        }

         ///<summary> This global exception handler will be invoked in case of any exception.
         /// It can be used for multiple purposes, like logging. But, currently it is being
         /// used to invalidate the Client Credentials Cache.
         /// </summary>
         private void GlobalExceptionHandler(object sender, UnhandledExceptionEventArgs e) {
            if (e.ExceptionObject is Exception)
            {
                Exception exception = (Exception)e.ExceptionObject;
                if(exception.InnerException is ApiException 
                    && conn.Creds.AuthType == AuthType.CLIENT_CREDENTIALS) 
                {
                   ApiException apiException = (ApiException)exception.InnerException;
                   if(apiException.StatusCode == 400 || apiException.StatusCode == 401)
                   {
                       ClientCredentialsService.Instance.InvalidateCache(conn.Creds, conn.Host);
                   }     
                }
            }
        }
    }
}
