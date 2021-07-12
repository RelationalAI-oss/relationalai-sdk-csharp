using System;
using System.Threading;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Com.RelationalAI
{
    public class CloudIntegrationTests
    {

        [SetUp]
        public void Setup()
        {
        }

        public static CloudConnection CreateCloudConnection() {
            ManagementConnection mgmtConn = new ManagementConnection(
                scheme: "https",
                host: "azure-ssh.relationalai.com",
                port: 443,
                verifySSL: false
            );

            ComputeInfoProtocol provisionedCompute = null;
            bool requestedOrProvisioning = true;

            string dbname = "";

            while(requestedOrProvisioning) {
                requestedOrProvisioning = false;

                var computes = mgmtConn.ListComputes();

                foreach(var comp in computes) {
                    if( "PROVISIONED".Equals(comp.State) )
                    {
                        var databases = mgmtConn.ListDatabases();
                        foreach(var db in databases) {
                            if( comp.Name.Equals(db.Default_compute_name) )
                            {
                                return CreateCloudConnection(db.Name, comp.Name, mgmtConn);
                            }
                        }

                        provisionedCompute = comp;
                        break;
                    } else if( "REQUESTED".Equals(comp.State) || "PROVISIONING".Equals(comp.State) ) {
                        requestedOrProvisioning = true;
                    }
                }

                dbname = IntegrationTestsCommons.genDbname("testcsharpclient");

                if(!requestedOrProvisioning) {
                    if( provisionedCompute == null )
                    {
                        provisionedCompute = mgmtConn.CreateCompute(dbname, RAIComputeSize.XS);

                        requestedOrProvisioning = true;
                    }
                }

                if(requestedOrProvisioning)
                {
                    Console.WriteLine("An instance is being requested or provisioned. Will try again in 10 seconds.");
                    Thread.Sleep(10000);
                }
            }
            return CreateCloudConnection(dbname, provisionedCompute.Name, mgmtConn);
        }

        public static CloudConnection CreateCloudConnection(string dbname, string computeName, ManagementConnection mgmtConn) {
            return new CloudConnection(
                dbname,
                mgmtConn,
                computeName: computeName
            );

        }

        [Test]
        public void Test1()
        {
            IntegrationTestsCommons.RunAllTests(CreateCloudConnection);
        }
    }
}
