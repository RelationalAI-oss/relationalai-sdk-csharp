using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using Com.RelationalAI;

namespace RelationalAISamples
{
    class LocalWorkflow
    {
        public void runLocalWorkflow()
        {
            string dbname = "localcsharpdatabase";

            IDictionary<String, String> extraHeaders = new Dictionary<String, String>();
            extraHeaders.Add("header-1", "value-1");
            extraHeaders.Add("header-2", "value-2");

            LocalConnection conn = new LocalConnection(dbname, extraHeaders: extraHeaders);

            conn.CreateDatabase(true);

            conn.LoadCSV(
                rel: "edge_csv",
                schema: new CSVFileSchema("Int64", "Int64"),
                syntax: new CSVFileSyntax(header: new List<string>() { "src", "dest" }, delim: "|"),
                //path: "~/workspace/data/wiki-Vote2.txt"
                data: @"
                    30|31
                    33|30
                    32|31
                    34|35
                    35|32
                "
                );

            conn.Query(
                srcStr: @"
                    def vertex(id) = exists(pos: edge_csv(pos, :src, id) or edge_csv(pos, :dest, id))
                    def edge(a, b) = exists(pos: edge_csv(pos, :src, a) and edge_csv(pos, :dest, b))
                ",
                persist: new List<string>() { "vertex", "edge" },
                output: "edge"
            );
            // Jaccard Similarity Query
            string queryString = @"
                def uedge(a, b) = edge(a, b) or edge(b, a)
                def tmp(a, b, x) = uedge(x,a) and uedge(x,b) and a > b
                def jaccard_similarity(a,b,v) = (count[x : tmp(a,b,x)] / count[x: (uedge(a, x) or uedge(b, x)) and tmp(a,b,_)])(v)

                def result = jaccard_similarity
            ";

            var queryResult = conn.Query(
                srcStr: queryString,
                output: "result"
            );

            Console.WriteLine("==> Jaccard Similarity: " + JObject.FromObject(queryResult).ToString());

        }
    }
}
