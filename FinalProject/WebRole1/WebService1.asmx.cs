using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Services;

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
    public class WebService1 : System.Web.Services.WebService
    {
        public static Trie aTrie;
        public static Trie topTrie;
        public static int trieCount = 0;
        public static string lastTrieWord = "";

        [WebMethod]
        public void getTrieCount()
        {
            // Serialize and return results
            Context.Response.Clear();
            Context.Response.ContentType = "application/json";

            var jsonSerializer = new System.Web.Script.Serialization.JavaScriptSerializer();
            string json = jsonSerializer.Serialize(new string[]{""+trieCount, lastTrieWord});

            Context.Response.Write(json);
        }

        [WebMethod]
        public void startQuery(string s)
        {
            if (aTrie == null)
            {
                // Connect to the Storage account
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionString"));
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

                // Download and build the basic trie. Build it based off of all options
                CloudBlobContainer container = blobClient.GetContainerReference("wiki");

                string path = "";

                if (container.Exists())
                {
                    foreach (IListBlobItem item in container.ListBlobs(null, false))
                    {
                        if (item.GetType() == typeof(CloudBlockBlob))
                        {
                            CloudBlockBlob blob = (CloudBlockBlob)item;

                            path = System.IO.Path.GetTempFileName() + blob.Name;
                            using (var filestream = System.IO.File.OpenWrite(path))
                            {
                                blob.DownloadToStream(filestream);
                            }
                        }
                    }
                }

                aTrie = buildTrie(path);

                // Download and build a popularity tree. We will check this tree first.
                CloudBlobContainer container2 = blobClient.GetContainerReference("topwiki");

                string path2 = "";

                if (container2.Exists())
                {
                    foreach (IListBlobItem item in container2.ListBlobs(null, false))
                    {
                        if (item.GetType() == typeof(CloudBlockBlob))
                        {
                            CloudBlockBlob blob = (CloudBlockBlob)item;

                            path2 = System.IO.Path.GetTempFileName() + blob.Name;
                            using (var filestream = System.IO.File.OpenWrite(path2))
                            {
                                blob.DownloadToStream(filestream);
                            }
                        }
                    }
                }

                topTrie = buildTrie(path2);
            }

            // Here is the empty results list
            LinkedList<string> results = new LinkedList<string>();

            // Add to results until we have 10
            while (results.Count < 10)
            {
                // Check Popular Results
                string[] st2 = topTrie.getGuesses(s.ToLower());

                // Transfer the popular array
                for (int i = 0; i < st2.Length; i++)
                {
                    if (results.Count < 10)
                    {
                        if (!results.Contains(st2[i]))
                        {
                            results.AddLast(st2[i]);
                        }
                    }
                }

                // If not enough suggestions, Check All Results
                if (st2.Length < 10)
                {
                    string[] st = aTrie.getGuesses(s.ToLower());

                    // Transfer the all array
                    for (int i = 0; i < st.Length; i++)
                    {
                        if (results.Count < 10)
                        {
                            if (!results.Contains(st[i]))
                            {
                                results.AddLast(st[i]);
                            }
                        }
                    }
                }

                // Take away one letter from user's search
                s = s.Substring(0, s.Length - 1);
            }

            // Place results into an array
            string[] result = results.ToArray();

            // Serialize and return results
            Context.Response.Clear();
            Context.Response.ContentType = "application/json";

            var jsonSerializer = new System.Web.Script.Serialization.JavaScriptSerializer();
            string json = jsonSerializer.Serialize(result);

            Context.Response.Write(json);
        }

        // Builds a Trie from the given path using a hybrid list/trie structure
        private Trie buildTrie(string path)
        {
            Trie aTrie = new Trie();

            var file = new StreamReader(path);

            string line = file.ReadLine();

            // Create the tree unless you run out of memory (Should not happen)
            try
            {
                while (line != null)
                {
                    aTrie.addWord(line.ToLower());

                    trieCount = trieCount + 1;
                    lastTrieWord = line;

                    line = file.ReadLine();
                }
            }
            catch (OutOfMemoryException)
            {
                return aTrie;
            }

            return aTrie;
        }
    }

    // Create a Trie datastructure
    // My use of "Word" refers to a full string, meaning it may include spaces
    public class Trie
    {
        private TrieNode root;
        private List<string> wordsFound;

        // Constructor declares the root
        public Trie()
        {
            root = new TrieNode();
        }

        // Given a string, return the best suggestions that this trie can offer
        public string[] getGuesses(string s)
        {
            if (s == "")
            {
                return new string[0];
            }

            wordsFound = new List<string>();

            Tuple<TrieNode, int> data = getCurrentNode(s, root, 0);

            TrieNode current = data.Item1;
            int index = data.Item2;

            // Special Case, if the current node is using a list
            if (current.isList())
            {
                if (current.isWord())
                {
                    if (index == s.Length)
                    {
                        wordsFound.Add(s);
                    }
                }

                // Check each word for matching letters
                foreach (string word in current.getList())
                {
                    if (word.StartsWith(s.Substring(index, s.Length - index)))
                    {
                        wordsFound.Add(s.Substring(0, index) + word);
                        if (wordsFound.Count >= 10)
                        {
                            break;
                        }
                    }
                }
            }
            else
            {
                // If its not using a list
                if (index == s.Length)
                {
                    depthFirst(current, s.Substring(0, s.Length - 1));
                }
            }

            return wordsFound.ToArray();
        }

        // adds onto the Words Found list in a depth first pattern through the trie
        private void depthFirst(TrieNode root, string wordSoFar)
        {
            wordSoFar += root.getChar();

            if (root.isWord())
            {
                wordsFound.Add(wordSoFar);
            }

            if (root.isList())
            {
                foreach (string word in root.getList())
                {
                    wordsFound.Add(wordSoFar + word);

                    if (wordsFound.Count == 10)
                    {
                        break;
                    }
                }
            }
            else
            {
                foreach (TrieNode child in root.getChildren())
                {
                    depthFirst(child, wordSoFar);

                    if (wordsFound.Count == 10)
                    {
                        break;
                    }
                }
            }
        }

        // Gives you the node you should be at for a given string
        // Returns the node, and what index of the string the node is at
        private Tuple<TrieNode, int> getCurrentNode(string s, TrieNode root, int i)
        {
            if (i == s.Length)
            {
                return Tuple.Create(root, i);
            }

            // Check if the TrieNode has a child with the specific char
            if (root.getChildren() != null)
            {
                foreach (TrieNode n in root.getChildren())
                {
                    // If we find it, continue with the next letter
                    if (n.getChar() == s[i])
                    {
                        return getCurrentNode(s, n, i + 1);
                    }
                }
            }

            return Tuple.Create(root, i);
        }

        // Adds a word to the Trie
        public void addWord(string word)
        {
            addWord(word, root);
        }

        public void addWord(string word, TrieNode aRoot)
        {
            TrieNode current = aRoot;

            // Loop through each letter in the word, placing it in the trie
            for (int i = 0; i < word.Length; i++)
            {
                bool found = false;

                // Is this node using a List?
                if (current.isList())
                {
                    // We need to give it the rest of this word
                    string curWord = "";
                    for (int j = i; j < word.Length; j++)
                    {
                        curWord = curWord + word[j];
                    }
                    current.addChild(curWord);
                    if (current.words.Count >= 50)
                    {
                        // Initalize the new trie
                        current.children = new List<TrieNode>();

                        // Store the words in the list
                        List<string> temp = current.words;

                        // This notifys the code that this node is no longer using a list
                        current.words = null;
                        foreach (string aWord in temp)
                        {
                            addWord(aWord, current);
                        }
                    }
                    break;
                }
                // If it's not using a list
                else
                {
                    // Check if the TrieNode has a child with the specific char
                    foreach (TrieNode n in current.getChildren())
                    {
                        // If we find it, continue with the next letter
                        if (n.getChar() == word[i])
                        {
                            current = n;
                            found = true;
                            break;
                        }
                    }

                    // If we didnt find it, make a node
                    if (!found)
                    {
                        TrieNode newNode = new TrieNode(word[i], (i == word.Length - 1));
                        current.addChild(newNode);
                        current = newNode;
                    }
                }
            }
        }

        // TrieNode is the node for the Trie class
        public class TrieNode
        {
            public List<string> words;
            public List<TrieNode> children;
            private Boolean word;
            private char ch;

            // Root Node
            public TrieNode()
            {
                children = new List<TrieNode>();
                word = false;
            }

            // All other Nodes
            public TrieNode(char c, bool aWord)
            {
                words = new List<string>();
                ch = c;
                word = aWord;
            }

            // Two addChild methods for list vs trie
            public void addChild(TrieNode n)
            {
                children.Add(n);
            }

            public void addChild(string s)
            {
                words.Add(s);
            }

            // Simple get methods
            public char getChar()
            {
                return ch;
            }

            public bool isWord()
            {
                return word;
            }

            public List<TrieNode> getChildren()
            {
                return children;
            }

            public List<string> getList()
            {
                return words;
            }

            // Returns if this node uses a list
            public bool isList()
            {
                return (words != null);
            }
        }
    }
}
