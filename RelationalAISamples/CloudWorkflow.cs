using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using Com.RelationalAI;
using IniParser.Model;
using Newtonsoft.Json.Linq;

namespace RelationalAISamples
{
    class CloudWorkflow
    {
        ManagementConnection MngtConn;
        CloudConnection CloudConn;
        string ComputeName;
        string ComputeId;

        int MaxAttempts;
        int SleepTime;

        public CloudWorkflow(string computeName = "csharpcompute-2021-02-17-1", string profile = "default", int maxAttempts = 20, int sleepTime = 60000)
        {
            // Loads data from ~/.rai/config (rai cloud configuration)
            IniData ini = Config.LoadDotRaiConfig();
            this.MaxAttempts = maxAttempts;
            this.SleepTime = sleepTime;
            this.ComputeName = computeName;

            this.MngtConn = new ManagementConnection(
                creds: RAICredentials.FromFile(profile: profile),
                scheme: "https",
                host: Config.RaiGetHost(ini, profile),
                port: 443,
                verifySSL: false
            );

            this.MngtConn.DebugLevel = 1;

            this.CloudConn = new CloudConnection(
                dbname: computeName,
                managementConn: this.MngtConn,
                computeName: computeName
            );

            this.CloudConn.DebugLevel = 1;
        }

        /*
         * Cloud workflow using RAICloud
         *
        */
        public void runCloudWorkflow()
        {
            // list computes for the current account
            /* Expected output: {
            "compute_requests_list": [
            {
                "id": "5170d1ac-e5f4-4547-a4f4-37dbca58048a",
                "accountName": "relationalai-db",
                "createdBy": "924c1f35-5062-4e78-9ce9-10157c29dc61",
                "computeName": "csharpcompute",
                "computeSize": "XS",
                "computeRegion": "us-east",
                "requestedOn": "2020-08-27T15:50:18.488Z",
                "createdOn": "2020-08-27T15:52:10.040Z",
                "deletedOn": "2020-08-27T23:38:34.062Z",
                "computeState": "DELETED",
                "computeId": "b4d958cf-8772-48f4-8b2c-7a37bdd415b5",
                "_etag": "\"0a002297-0000-0100-0000-5f4d07c30000\""
            }, ... ]}
              */
            var computes = this.MngtConn.ListComputes();
            Console.WriteLine("==> Computes:");
            foreach( var c in computes)
            {
                Console.WriteLine(JObject.FromObject(c));
            }

            var isCompute = false;

            // list databases for the current account
            /* Expected output: {
            "databases": [
                {
                  "account_name": "relationalai-db",
                  "name": "csharpdbtest",
                  "region": "us-east",
                  "database_id": "a659009a-dae8-4a90-8438-41f5ac6bbb2d",
                  "status": "CREATED"
                },
                {
                  "account_name": "relationalai-db",
                  "name": "csharpcompute2",
                  "region": "us-east",
                  "database_id": "05e90e99-5601-4630-8696-5656eb0b31d2",
                  "status": "CREATED"
                }, ... ]}
            */
            var databases = this.MngtConn.ListDatabases();
            Console.WriteLine("==> Databases:");
            foreach(var database in databases)
            {
                Console.WriteLine(JObject.FromObject(database).ToString());
                Console.WriteLine(database.State);
            }

            // list users for the current account
            /* Expected output: {
              "users": [
                  {
                    "account_name": "account_name",
                    "username": "username",
                    "first_name": "firstname",
                    "last_name": "lastname",
                    "email": "user@relational.ai",
                    "status": "ACTIVE",
                    "access_key1": "xxxxxxxxxxxxxxxxxxxxxx"
                  }, ...
                ]}
            */
            var users = this.MngtConn.ListUsers();
            Console.WriteLine("==> Users:");
            foreach(var user in users)
            {
                Console.WriteLine(JObject.FromObject(user).ToString());
            }

            // create compute
            var compute = GetComputeByName(this.MngtConn, this.ComputeName);
            
            try {
                if (compute == null)
                {
                    var createComputeResponse = this.MngtConn.CreateCompute(computeName: ComputeName, size: RAIComputeSize.XS);
                    isCompute = true;
                    this.ComputeId = createComputeResponse.Id;
                    Console.WriteLine("=> Create compute response: " + JObject.FromObject(createComputeResponse).ToString());
                } else
                {
                    this.ComputeId = compute.Id;
                    Console.WriteLine($"==> Compute {this.ComputeName} is used.");
                }

                // wait for compute to be provisioned
                // a compute is a single tenant VM used for the current account (provisioning time ~ 5 mins)
                if(!WaitForCompute(this.MngtConn, this.ComputeName))
                    return;

                // create database with the name as specificied in the MngtConnection
                this.CloudConn.CreateDatabase(overwrite: true);

                this.CloudConn.LoadCSV(
                    // import data into edge_csv relation
                    rel: "edge_csv",
                    // data type mapping
                    schema: new CSVFileSchema("Int64", "Int64"),
                    syntax: new CSVFileSyntax(header: new List<string>() { "src", "dest" }, delim: "|"),
                    // data imported over the wire
                    // alternative options are to specify a datasource that is a path for an azure blob storage file
                    data: @"
                            30|31
                            33|30
                            32|31
                            34|35
                            35|32
                        "
                );

                // persisting vertex and edges for future computations
                var edges = this.CloudConn.Query(
                    srcStr: @"
                        def vertex(id) = exists(pos: edge_csv(pos, :src, id) or edge_csv(pos, :dest, id))
                        def edge(a, b) = exists(pos: edge_csv(pos, :src, a) and edge_csv(pos, :dest, b))
                    ",
                    persist: new List<string>() { "vertex", "edge" },
                    // this the result of the query
                    output: "edge"
                );

                Console.WriteLine("==> Query output: " + JObject.FromObject(edges).ToString());

                // Jaccard Similarity query
                string queryString = @"
                    def uedge(a, b) = edge(a, b) or edge(b, a)
                    def tmp(a, b, x) = uedge(x,a) and uedge(x,b) and a > b
                    def jaccard_similarity(a,b,v) = (count[x : tmp(a,b,x)] / count[x: (uedge(a, x) or uedge(b, x)) and tmp(a,b,_)])(v)

                    def result = jaccard_similarity
                ";

                var queryResult = this.CloudConn.Query(
                    srcStr: queryString,
                    // query output
                    output: "result"
                );

                Console.WriteLine("=> Jaccard Similarity query result: " + JObject.FromObject(queryResult).ToString());

                var events = this.MngtConn.ListComputeEvents(this.ComputeId);
                foreach(var e in events) {
                    Console.WriteLine("=> Compute event: " + JObject.FromObject(e).ToString());
                }

                var usage = this.MngtConn.GetAccountCreditUsage();
                Console.WriteLine("=> Account Credit Usage: " + JObject.FromObject(usage).ToString());


                Console.WriteLine($"Press 'Y' to destroy {this.ComputeName}");
                ConsoleKeyInfo cki = Console.ReadKey();

                if (cki.Key.ToString() == "Y")
                {
                    CleanCompute();
                }
                else 
                { 
                    Console.WriteLine("Nothing to do."); 
                }
            }

            catch (Exception ex){
                Console.WriteLine (ex);
                // Delete the compute if there is any exception after creating it.
                if (isCompute) 
                {
                    CleanCompute();
                }
            }
        }

        /*
         * Helpers
         *
         */
         private void CleanCompute() {
            // remove default compute (disassociate database from compute)
            this.MngtConn.RemoveDefaultCompute(dbname: this.ComputeName);

            // delete compute => stop charging for the compute
            this.MngtConn.DeleteCompute(computeName: ComputeName);
         }

        private bool WaitForCompute(ManagementConnection connection, string computeName)
        {
            for (var i=0; i<this.MaxAttempts; i++)
            {
                var compute = GetComputeByName(connection, computeName);
                Console.WriteLine($"==> Compute {computeName} state: {compute.State}");
                if ("PROVISIONED".Equals(compute.State))
                    return true;
                Thread.Sleep(this.SleepTime);
            }
            return false;
        }

        private ComputeInfoProtocol GetComputeByName(ManagementConnection connection, string computeName)
        {
            var filters = new RAIComputeFilters(null, name: new List<string> {computeName, "random"}, null, null);
            var computes = connection.ListComputes(filters);
            return computes.FirstOrDefault();
        }
    }
}
