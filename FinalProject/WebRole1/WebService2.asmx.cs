using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Script.Services;
using System.Web.Services;
using System.Text.RegularExpressions;

// Ross Grambo
// INFO 344
// Web Crawler
// May 19, 2015

namespace WebRole1
{
    /// <summary>
    /// Summary description for WebService1
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    [System.Web.Script.Services.ScriptService]
    public class WebService2 : System.Web.Services.WebService
    {
        public Dictionary<string, string> cache;

        // Reads the last posted status of the worker role
        [WebMethod]
        public void GetStatus()
        {
            string[] result = new string[7];
            try
            {
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
                CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                CloudTable table = tableClient.GetTableReference("workertable");
                table.CreateIfNotExists();

                // Send query
                TableQuery<WorkerEntity> aQuery = new TableQuery<WorkerEntity>()
                    .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "Worker"));

                List<WorkerEntity> aList = new List<WorkerEntity>();

                // There should only be one
                foreach (WorkerEntity entity in table.ExecuteQuery(aQuery))
                {
                    result[0] = entity.status;
                    result[1] = entity.counter;
                    result[2] = entity.urlcount.ToString();
                    result[3] = entity.lasturls;
                    result[4] = entity.queuesize.ToString();
                    result[5] = entity.indexsize.ToString();
                    result[6] = entity.error;
                }

            }
            catch (Exception e)
            {

            }

            // Serialize and return results
            Context.Response.Clear();
            Context.Response.ContentType = "application/json";

            var jsonSerializer = new System.Web.Script.Serialization.JavaScriptSerializer();
            string json = jsonSerializer.Serialize(result);

            Context.Response.Write(json);
        }

        // Adds the given url to the queue
        private void AddToQueue(string url)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);

            // Ensure Queue Exists
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            CloudQueue queue = queueClient.GetQueueReference("urlqueue");
            queue.CreateIfNotExists();

            //Add message
            CloudQueueMessage message = new CloudQueueMessage(url);
            queue.AddMessage(message);
        }

        // Reads from the table looking for the given url
        [WebMethod]
        public void ReadWithWord(string search)
        {
            Regex rgx = new Regex("[^a-zA-Z0-9 -]");
            string fixedSearch = rgx.Replace(search, "");

            // Declare result
            string json;

            if (cache == null)
            {
                cache = new Dictionary<string, string>();
            }

            // Check if we have the search cached
            if (cache.ContainsKey(fixedSearch))
            {
                json = cache[fixedSearch];
            }
            else
            {
                List<UrlEntity> list = new List<UrlEntity>();

                foreach (string word in fixedSearch.Split(' '))
                {
                    // Retrieve the storage account from the connection string.
                    CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
                    CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                    CloudTable table = tableClient.GetTableReference("urltable");
                    table.CreateIfNotExists();

                    // Create a retrieve operation that takes a customer entity.
                    TableQuery<UrlEntity> rangeQuery = new TableQuery<UrlEntity>()
                        .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, word.ToLower()));

                    list.AddRange(table.ExecuteQuery(rangeQuery));
                
                }

                var result = list.GroupBy(x => x.RowKey )
                    .Select(group => new
                    {
                        Url = group.Key,
                        Title = group.ToList().First().title,
                        Date = group.ToList().First().date,
                        Count = group.Count(),
                        Img = group.ToList().First().img
                    })
                    .OrderByDescending(x => x.Count)
                    .Take(20);

                // Serialize and return results
                Context.Response.Clear();
                Context.Response.ContentType = "application/json";

                var jsonSerializer = new System.Web.Script.Serialization.JavaScriptSerializer();
                json = jsonSerializer.Serialize(result);

                cache.Add(fixedSearch, json);
            }

            Context.Response.Write(json);
        }

        // A method for the user to call
        [WebMethod]
        public void StartWithUrl(string url)
        {
            AddToQueue(url);
        }

        // Starts by reading robot.txt
        [WebMethod]
        public void Start()
        {
            // Read robots.txt
            WebClient client = new WebClient();
            Stream stream = client.OpenRead("http://www.cnn.com/robots.txt");
            StreamReader reader = new StreamReader(stream);
            string content = reader.ReadToEnd();

            // Split it up into lines
            string[] lines = content.Split('\n');

            // Read and handle each line
            foreach (string line in lines)
            {
                if (line.StartsWith("Sitemap: "))
                {
                    AddToQueue(line.Substring(9));
                }
            }

            // Read robots.txt of Bleacher
            Stream stream2 = client.OpenRead("http://bleacherreport.com/robots.txt");
            StreamReader reader2 = new StreamReader(stream2);
            string content2 = reader2.ReadToEnd();

            // Split it up into lines
            string[] lines2 = content2.Split('\n');

            // Read and handle each line
            foreach (string line in lines2)
            {
                if (line.StartsWith("Sitemap: "))
                {
                    AddToQueue(line.Substring(9));
                }
            }

            cache = new Dictionary<string,string>();
        }

        // Clears all data and commands worker to stop
        [WebMethod]
        public void Clear()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);

            // Ensure WorkerQueue Exists
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            CloudQueue queue = queueClient.GetQueueReference("workerqueue");
            queue.CreateIfNotExists();

            //Add STOP message
            CloudQueueMessage message = new CloudQueueMessage("STOP");
            queue.AddMessage(message);

            // Ensure Queue Exists
            CloudQueueClient queueClient2 = storageAccount.CreateCloudQueueClient();
            CloudQueue queue2 = queueClient2.GetQueueReference("urlqueue");
            queue2.CreateIfNotExists();

            queue2.Clear();

            // 
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference("urltable");

            table.DeleteIfExists();
        }
    }
}