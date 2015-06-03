using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.Queue;
using HtmlAgilityPack;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;

// Ross Grambo
// INFO 344
// Web Crawler
// May 19, 2015

namespace WorkerRole1
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        CloudTable table;
        CloudTable statustable;
        CloudQueue queue;
        CloudQueue workerqueue;

        PerformanceCounter ramcounter;
        PerformanceCounter cpucounter;

        List<string> disallow;
        List<string> disallowB;
        HashSet<string> urlsvisited;
        Queue<string> lasturlsvisited;

        bool stopping = false;

        static int tablecount = 0;
        static string errors = "";

        // Code that the worker runs
        public override void Run()
        {
            while (true)
            {
                try
                {
                    Thread.Sleep(50);

                    // Check for a stop command
                    checkCommand();

                    // Continue looking in the queue
                    CloudQueueMessage message = queue.GetMessage(TimeSpan.FromMinutes(5));

                    // If there is a url in the queue
                    if (message != null)
                    {
                        {
                            // Find and store all of the urls at the given url
                            StoreAllUrls(message.AsString);

                            // Delete the message
                            try
                            {
                                queue.DeleteMessage(message);
                            }
                            catch (StorageException e)
                            {
                                errors = errors + e.Message + '\n';
                            }
                        }
                    }
                    else
                    {
                        statusUpdate("Idle");

                        stopping = false;
                    }
                }
                catch (Exception e)
                {
                    errors = errors + e.Message + '\n';
                }
            }
        }

        // Checks for a stop command
        public void checkCommand()
        {
            CloudQueueMessage command = workerqueue.GetMessage(TimeSpan.FromMinutes(5));

            if (command != null)
            {
                stopping = true;

                statusUpdate("Stopping");

                workerqueue.DeleteMessage(command);

                queue.Clear();

                urlsvisited = new HashSet<string>();
                lasturlsvisited = new Queue<string>();
            }
        }

        // Stores the worker's status and other info
        public void statusUpdate(string st)
        {
            double lastRAM = ramcounter.NextValue();

            double lastCPU = cpucounter.NextValue();

            queue.FetchAttributes();
            int q1 = (int)queue.ApproximateMessageCount;

            WorkerEntity aSt = new WorkerEntity(st, lastRAM.ToString() + "~~" + lastCPU.ToString(), urlsvisited.Count, string.Join("<br>", lasturlsvisited.ToArray()), q1, tablecount, errors);

            TableOperation insertOperation = TableOperation.InsertOrReplace(aSt);
            statustable.Execute(insertOperation);

        }

        // Initializes when the worker starts
        public override bool OnStart()
        {
            urlsvisited = new HashSet<string>();
            disallow = new List<string>();
            disallowB = new List<string>();
            lasturlsvisited = new Queue<string>();

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionString"));

            // Ensure Table Exists
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            table = tableClient.GetTableReference("urltable");
            table.CreateIfNotExists();

            // Ensure Queue Exists
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            queue = queueClient.GetQueueReference("urlqueue");
            queue.CreateIfNotExists();

            // Ensure Worker Status Table Exists
            CloudTableClient tableClient2 = storageAccount.CreateCloudTableClient();
            statustable = tableClient.GetTableReference("workertable");
            statustable.CreateIfNotExists();

            // Ensure Worker Queue Exists
            CloudQueueClient queueClient2 = storageAccount.CreateCloudQueueClient();
            workerqueue = queueClient2.GetQueueReference("workerqueue");
            workerqueue.CreateIfNotExists();

            // Start up counters
            ramcounter = new PerformanceCounter("Memory", "Available MBytes");
            cpucounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");

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
                if (line.StartsWith("Disallow: "))
                {
                    disallow.Add(line.Substring(10));
                }
            }

            Stream stream2 = client.OpenRead("http://bleacherreport.com/robots.txt");
            StreamReader reader2 = new StreamReader(stream);
            string content2 = reader.ReadToEnd();

            // Split it up into lines
            string[] lines2 = content.Split('\n');

            // Read and handle each line
            foreach (string line in lines2)
            {
                if (line.StartsWith("Disallow: "))
                {
                    disallowB.Add(line.Substring(10));
                }
            }


            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.

            bool result = base.OnStart();

            Trace.TraceInformation("WorkerRole1 has been started");

            return result;
        }

        // Adds the url to the queue
        public void AddToQueue(string url)
        {
            if (!stopping)
            {
                //Add message
                CloudQueueMessage message = new CloudQueueMessage(url);
                queue.AddMessage(message);
            }
        }

        // Indexes the url title and date
        public void AddToTable(string url, string title, string date, string img)
        {
            if (!stopping)
            {
                Regex rgx = new Regex("[^a-zA-Z0-9 -]");
                string fixedTitle = rgx.Replace(title, "");

                string[] words = fixedTitle.Split(new char[] { ' ' });

                foreach (string word in words)
                {
                    if (word != "" && word != "-")
                    {
                        // Add to table
                        UrlEntity aUrl = new UrlEntity(HttpUtility.UrlEncode(url), title, date, word.ToLower(), HttpUtility.UrlEncode(img));

                        TableOperation insertOperation = TableOperation.Insert(aUrl);
                        try
                        {
                            table.Execute(insertOperation);
                        }
                        catch (StorageException e)
                        {

                        }
                    }
                }

                tablecount++;

                lasturlsvisited.Enqueue(url);

                if (lasturlsvisited.Count > 10)
                {
                    lasturlsvisited.Dequeue();
                }
            }
        }

        // This is the main "Crawl"
        // It handles the page differently depending on XML vs HTML
        public void StoreAllUrls(string url)
        {
            // Test if its HTML
            if (url.Contains(".html") || url.Contains(".htm"))
            {
                statusUpdate("Crawling");

                HtmlWeb web = new HtmlWeb();
                HtmlDocument document = web.Load(url);

                string title = "";
                string date = "";
                string img = "";

                // Get title
                var titlenodes = document.DocumentNode.Descendants("title");

                foreach (var node in titlenodes)
                {
                    title = node.InnerText;
                }

                // Get date
                var datenodes = document.DocumentNode.Descendants("p")
                    .Where(d => d.Attributes.Contains("class") && d.Attributes["class"].Value == "update-time");

                foreach (var node in datenodes)
                {
                    date = node.InnerHtml.Substring(8);
                }

                // Get title
                var imgnodes = document.DocumentNode.Descendants("img");

                foreach (var node in imgnodes)
                {
                    img = node.Attributes["src"].Value;
                }

                AddToTable(url, title, date, img);

                // Get all other urls
                var nodes = document.DocumentNode.Descendants("a")
                    .Where(d => d.Attributes.Contains("href") && CheckUrl(d.Attributes["href"].Value));

                int count = 0;
                string aUrl;

                // Add each of them to the queue
                foreach (var node in nodes)
                {
                    // Check for stop command
                    count++;
                    if (count % 10 == 0)
                    {
                        checkCommand();
                    }

                    if (stopping)
                    {
                        break;
                    }

                    aUrl = node.Attributes["href"].Value;

                    if (!urlsvisited.Contains(aUrl))
                    {
                        AddToQueue(aUrl);

                        urlsvisited.Add(aUrl);

                        statusUpdate("Crawling");
                    }
                }
            }
            // Test if its XML
            else if (url.Contains(".xml"))
            {
                statusUpdate("Loading");

                XmlDocument doc1 = new XmlDocument();
                doc1.Load(url);
                XmlElement root = doc1.DocumentElement;

                int count = 0;

                foreach (XmlNode node in root)
                {
                    // Check for stop command
                    count++;
                    if (count >= 10)
                    {
                        checkCommand();

                        count = 0;
                    }

                    if (stopping)
                    {
                        break;
                    }

                    if (node["lastmod"] == null)
                    {
                        if (node["news:news"] == null)
                        {
                            if (CheckUrl(node["loc"].InnerText))
                            {
                                AddToQueue(node["loc"].InnerText);
                            }
                        }
                        else
                        {
                            if (DateTime.Compare(DateTime.Parse(node["news:news"]["news:publication_date"].InnerText.Substring(0, 10)), new DateTime(2015, 4, 1)) > 0)
                            {
                                if (CheckUrl(node["loc"].InnerText))
                                {
                                    AddToQueue(node["loc"].InnerText);
                                }
                            }
                        }
                    }
                    else
                    {
                        if (DateTime.Compare(DateTime.Parse(node["lastmod"].InnerText.Substring(0, 10)), new DateTime(2015, 4, 1)) > 0)
                        {
                            if (CheckUrl(node["loc"].InnerText))
                            {
                                AddToQueue(node["loc"].InnerText);
                            }
                        }
                    }
                }
            }
        }

        // Does the full check on a url to see if it is acceptable and new
        public bool CheckUrl(string url)
        {
            // Have we already seen the url?
            if (urlsvisited.Contains(url))
            {
                return false;
            }

            // Is it a CNN url?
            Regex regex = new Regex(@"http:\/\/.*\.cnn\.com\/.*");
            Match match = regex.Match(url);
            if (match.Success)
            {

                // Is it an allowed url?
                foreach (string bad in disallow)
                {
                    if (url.Contains(bad))
                    {
                        return false;
                    }
                }

                return true;
            }
            else
            {
                // Then is it NBA related?
                Regex regex2 = new Regex(@"http:\/\/.*\.bleacherreport\.com\/.*");
                Match match2 = regex.Match(url);
                if (match2.Success)
                {
                    // Is it not nba related?
                    if (!url.Contains("nba"))
                    {
                        return false;
                    }

                    // Is it an allowed url?
                    foreach (string bad in disallowB)
                    {
                        if (url.Contains(bad))
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }
            return false;
        }

        // Calls this function when the worker stops
        public override void OnStop()
        {
            Trace.TraceInformation("WorkerRole1 is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("WorkerRole1 has stopped");
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following with your own logic.
            while (!cancellationToken.IsCancellationRequested)
            {
                Trace.TraceInformation("Working");
                await Task.Delay(1000);
            }
        }
    }
}
