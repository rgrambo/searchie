using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.WindowsAzure.Storage.Table;

namespace WorkerRole1
{
    public class UrlEntity : TableEntity
    {
        public UrlEntity(string aurl, string atitle, string adate, string aword, string aimg)
        {
            this.PartitionKey = aword;
            this.RowKey = aurl;
            title = atitle;
            date = adate;
            img = aimg;
        }

        public UrlEntity() { }

        public string date { get; set; }
        public string title { get; set; }
        public string img { get; set; }
    }

    public class WorkerEntity : TableEntity
    {
        public WorkerEntity(string s, string c, int u, string l, int q, int i, string e)
        {
            this.PartitionKey = "Worker";
            this.RowKey = "Status";
            status = s;
            counter = c;
            urlcount = u;
            lasturls = l;
            queuesize = q;
            indexsize = i;
            error = e;
        }

        public WorkerEntity() { }

        public string status { get; set; }
        public string counter { get; set; }
        public int urlcount { get; set; }
        public string lasturls { get; set; }
        public int queuesize { get; set; }
        public int indexsize { get; set; }
        public string error { get; set; }
    }
}