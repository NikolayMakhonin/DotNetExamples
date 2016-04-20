using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using JobSearch.Classes.Filter;
using Logger;
using Logger.AsyncProcess;
using Logger.Utils;
using Utils.Compilers;
using Utils.Contracts.Patterns;
using Utils.Serialization;

namespace JobSearch.Classes
{
    public class JobSearcher: IStreamSerializable, IModified
    {
        private readonly AsyncProcessor _asyncProcessor;

        private static readonly CookieContainer _cookies;
        private static readonly CSharpCompiler _compiler;

        static JobSearcher()
        {
            _cookies = new CookieContainer();

            _compiler = new CSharpCompiler();
            _compiler.UsingTypes.Add(typeof(JobSearcher));
            _compiler.UsingTypes.Add(typeof(Regex));
            _compiler.UsingTypes.Add(typeof(Encoding));
            _compiler.UsingTypes.Add(typeof(HttpUtility));
            _compiler.UsingTypes.Add(typeof(Site));
            _compiler.UsingTypes.Add(typeof(Request));
            _compiler.UsingTypes.Add(typeof(EncodingExt));
            _compiler.UsingTypes.Add(typeof(Log));
            _compiler.UsingTypes.Add(typeof(IStreamSerializable));

            ServicePointManager.ServerCertificateValidationCallback = AcceptAllCertifications;
        }

        public JobSearcher(AsyncProcessor asyncProcessor)
        {
            _asyncProcessor = asyncProcessor;

            _sites = new SortedList<Site>(false, false);
            _results = new SortedList<Result>(true, true);
            _blockedResults = new SortedList<Result>(true, true);
            _favorites = new SortedList<Result>(true, true);
            _favorites2 = new SortedList<Result>(true, true);

            _sites.CollectionChanged += collectionChanged;
            _results.CollectionChanged += collectionChanged;
            _favorites.CollectionChanged += collectionChanged;
            _favorites2.CollectionChanged += collectionChanged;
            _blockedResults.CollectionChanged += collectionChanged;
        }

        void collectionChanged(object sender, CollectionChangedEventArgs<Site> e)
        {
            OnModified(false);
        }

        void collectionChanged(object sender, CollectionChangedEventArgs<Result> e)
        {
            OnModified(false);
        }

        public ILockerList<StringMatchFilter> Filters { get; set; }

        private readonly SortedList<Site> _sites;

        public ICollectionChangedList<Site> Sites
        {
            get { return _sites; }
        }

        private readonly SortedList<Result> _results;

        public ICollectionChangedList<Result> Results
        {
            get { return _results; }
        }

        private readonly SortedList<Result> _favorites;

        public ICollectionChangedList<Result> Favorites
        {
            get { return _favorites; }
        }

        private readonly SortedList<Result> _favorites2;

        public ICollectionChangedList<Result> Favorites2
        {
            get { return _favorites2; }
        }

        private readonly SortedList<Result> _blockedResults;

        public ICollectionChangedList<Result> BlockedResults
        {
            get { return _blockedResults; }
        }

        public void Search()
        {
            Results.Clear();
            lock (Sites.Locker)
            {
                foreach (var site in Sites)
                {
                    if (!site.Enabled) continue;
                    lock (site.Requests.Locker)
                    {
                        foreach (var requestParams in site.Requests)
                        {
                            if (!requestParams.Enabled) continue;
                            _asyncProcessor.AddAction(getParseUrlAction(requestParams, site));
                        }
                    }
                }
            }
        }

        public void MoveResultsTo(ILockerList<Result> from, ILockerList<Result> to)
        {
            lock (from.Locker)
            {
                for (int i = from.Count - 1; i >= 0; i--)
                {
                    var result = from[i];
                    if (!result.Selected) continue;
                    from.Remove(result);
                    result.Selected = false;
                    to.Add(result);
                }
            }
        }

        public void SelectNone(ICollectionChangedList<Result> results)
        {
            lock (results.Locker)
            {
                foreach (var result in results)
                {
                    if (!result.Selected) continue;
                    result.Selected = false;
                    results.OnItemModified(results.IndexOf(result));
                }
            }
        }

        public void SelectAll(ICollectionChangedList<Result> results)
        {
            lock (results.Locker)
            {
                foreach (var result in results)
                {
                    if (result.Selected) continue;
                    result.Selected = true;
                    results.OnItemModified(results.IndexOf(result));
                }
            }
        }

        public void OpenInBrowser(ILockerList<Result> results)
        {
            lock (results.Locker)
            {
                foreach (var result in results)
                {
                    if (!result.Selected) continue;
                    System.Diagnostics.Process.Start(result.Url);
                }
            }
        }

        public void DeleteSelected(ILockerList<Result> results)
        {
            lock (results.Locker)
            {
                for (int i = results.Count - 1; i >= 0; i--)
                {
                    if (results[i].Selected) results.RemoveAt(i);
                }
            }
        }

        public void ReFilter(ILockerList<Result> results)
        {
            lock (results.Locker)
            {
                for (int i = results.Count - 1; i >= 0; i--)
                {
                    var result = results[i];
                    if (!filter(result, Filters)) results.Remove(result);
                }
            }
        }

        public void ReFilter()
        {
            ReFilter(Results);
            ReFilter(Favorites);
            ReFilter(Favorites2);
            ReFilter(BlockedResults);
        }

        private Action getParseUrlAction(Request requestParams, Site site)
        {
            return () =>
            {
                var encoding = String.IsNullOrWhiteSpace(site.Encoding)
                    ? (requestParams.Encoding ?? Encoding.UTF8)
                    : Encoding.GetEncoding(site.Encoding);
                requestParams.Encoding = encoding;
                var contents = download(requestParams);
                try
                {
                    int cntErrors = 0;
                    foreach (var content in contents)
                    {
                        if (content == null)
                        {
                            Log.Add(RecType.UserError, "DownloadFunc return null");
                            cntErrors++;
                            if (cntErrors > 3) break;
                            continue;
                        }
                        cntErrors = 0;
                        parseContent(site, requestParams.Url, content);
                    }
                }
                catch (Exception exception)
                {
                    Log.Add(RecType.Error, exception);
                }
            };
        }

        const string codeTemplate = @"
        protected override IEnumerable<string> func(Request request)
        {{
{0}
            yield break;
        }}";

        private IEnumerable<string> download(Request requestParams)
        {
            if (String.IsNullOrWhiteSpace(requestParams.DownloadFuncCode))
            {
                return new [] { Download(requestParams) };
            }

            if (requestParams.DownloadFunc == null)
            {
                CompilerErrorCollection errors;
                RequestDownloadFuncBase obj;
                var code = String.Format(codeTemplate, requestParams.DownloadFuncCode);
                if (!_compiler.CompileClass(ref code, true, out obj, out errors))
                {
                    Log.Add(RecType.UserError, "Compile errors:\r\n" + CSharpCompiler.ErrorsToString(errors, code));
                    return null;
                }
                if (errors.Count > 0)
                {
                    Log.Add(RecType.UserWarning, "Compile errors:\r\n" + CSharpCompiler.ErrorsToString(errors, code));
                }
                requestParams.DownloadFunc = obj.Func;
            }

            if (requestParams.DownloadFunc == null) return null;

            var preparedRequestParams = (Request)requestParams.Clone();
            return requestParams.DownloadFunc(preparedRequestParams);
        }

        private static HttpWebRequest makeRequest(Request requestParams)
        {
            var request = (HttpWebRequest)WebRequest.Create(new Uri(requestParams.Url));
            request.UseDefaultCredentials = true;
            request.Timeout = 60000;
            request.ServicePoint.Expect100Continue = false; 
            request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/43.0.2357.81 Safari/537.36";
            request.Headers["Accept-Language"] = "ru,en-US;q=0.8,en;q=0.6";
            request.Headers["Accept-Encoding"] = "gzip, deflate";
            request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
            request.KeepAlive = true;
            if (!String.IsNullOrWhiteSpace(requestParams.Referer)) request.Referer = requestParams.Referer;
            if (!String.IsNullOrWhiteSpace(requestParams.Origin)) request.Headers["Origin"] = requestParams.Origin;
            request.CookieContainer = _cookies;
            if (requestParams.RequestType == RequestType.POST)
            {
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                if (!String.IsNullOrEmpty(requestParams.PostData))
                {
                    var data = EncodingExt.ANSI.GetBytes(requestParams.PostData);
                    request.ContentLength = data.Length;

                    using (var stream = request.GetRequestStream())
                    {
                        stream.Write(data, 0, data.Length);
                    } 
                }
            }
            return request;
        }

        public static String Download(Request requestParams)
        {
            try
            {
                var request = makeRequest(requestParams);
                var responce = (HttpWebResponse)request.GetResponse();
                if ((int) responce.StatusCode < 200 || (int) responce.StatusCode >= 300)
                {
                    Log.Add(RecType.Error, "Download url: " + requestParams.Url + "; Responce Code = " + (int)responce.StatusCode);
                    return null;
                }
                using (var stream = responce.GetResponseStream())
                {
                    if (stream == null)
                    {
                        Log.Add(RecType.Error, "Download url: " + requestParams.Url + "; stream == null");
                        return null;
                    }
                    var uncompressedStream = stream;
                    switch (responce.ContentEncoding.ToLower())
                    {
                        case "gzip":
                            uncompressedStream = new MemoryStream();
                            CompressionUtils.UnPack(stream, uncompressedStream, CompressMethod.TextGZip);
                            uncompressedStream.Position = 0;
                            break;
                        case "deflate":
                            uncompressedStream = new MemoryStream();
                            CompressionUtils.UnPack(stream, uncompressedStream, CompressMethod.TextDeflate);
                            uncompressedStream.Position = 0;
                            break;
                    }
                    var reader = new StreamReader(uncompressedStream, requestParams.Encoding, true);
                    var content = reader.ReadToEnd();
                    return content;
                }
            }
            catch (Exception exception)
            {
                Log.Add(RecType.Error, "Download url: " + requestParams.Url, exception);
                return null;
            }
        }

        private static readonly Regex _removeTagsRegex = new Regex(@"</?\w+( [^<>]*)?>");

        private void parseContent(Site site, string url, string content)
        {
            Match entriesMatch = new Regex(site.EntryRegEx, RegexOptions.IgnoreCase | RegexOptions.Singleline).Match(content);
            int entriesCount = 0;
            int filterCount = 0;
            var lastAcceptedFilters = new SortDict<StringMatchFilter, int>(CompareUtils.CompareHashCode);
            string lastDate = null;
            while (entriesMatch.Success)
            {
                entriesCount++;
                var entry = entriesMatch.Result(site.EntryRegExResult);
                var result = new Result();
                result.EntryName = firstMatch(entry, site.EntryNameRegEx, site.EntryNameRegExResult, true);
                result.Company = firstMatch(entry, site.CompanyRegEx, site.CompanyRegExResult, true);
                result.Description = firstMatch(entry, site.DescriptionRegEx, site.DescriptionRegExResult, true);
                result.Date = firstMatch(entry, site.DateRegEx, site.DateRegExResult, true);
                result.Cost = firstMatch(entry, site.CostRegEx, site.CostRegExResult, true);
                result.Url = firstMatch(entry, site.UrlRegEx, site.UrlRegExResult, false);
                result.Answers = firstMatch(entry, site.AnswersRegEx, site.AnswersRegExResult, false);
                result.SiteName = site.SiteName;

                if (!String.IsNullOrWhiteSpace(result.Date)) lastDate = result.Date;

                if (filter(result, Filters, lastAcceptedFilters)) 
                {
                    addResult(result);
                    filterCount++;
                }
                entriesMatch = entriesMatch.NextMatch();
            }

            var sb = new StringBuilder();
            foreach (var item in lastAcceptedFilters)
            {
                if (sb.Length > 0) sb.AppendLine();
                sb.Append('[').Append(item.Value).Append("] ")
                    .Append(item.Key.Permission == FilterPermission.Allow ? "+" : "-")
                    .Append(item.Key.ContentPart)
                    .Append(item.Key.Negative ? "-" : "+")
                    .Append(item.Key.Pattern);
            }
            Log.Add(RecType.Info, String.Format("Parse info: {0}, LastDate: {1}\r\nUrl: {2}\r\nEntries: {3}\r\nFilterEntries: {4}\r\n\r\nAccepted Filters:\r\n{5}", site.SiteName, lastDate, url, entriesCount, filterCount, sb));
        }

        private void addResult(Result result)
        {
            bool founded = false;
            lock (Results.Locker)
            {
                int index = Results.IndexOf(result);
                if (index >= 0)
                {
                    founded = true;
                    result.Selected = Results[index].Selected; 
                    Results[index] = result;
                }
            }
            lock (Favorites.Locker)
            {
                int index = Favorites.IndexOf(result);
                if (index >= 0)
                {
                    founded = true;
                    result.Selected = Favorites[index].Selected;
                    Favorites[index] = result;
                }
            }
            lock (Favorites2.Locker)
            {
                int index = Favorites2.IndexOf(result);
                if (index >= 0)
                {
                    founded = true;
                    result.Selected = Favorites2[index].Selected;
                    Favorites2[index] = result;
                }
            }
            lock (BlockedResults.Locker)
            {
                int index = BlockedResults.IndexOf(result);
                if (index >= 0)
                {
                    founded = true;
                    result.Selected = BlockedResults[index].Selected;
                    BlockedResults[index] = result;
                }
            }
            if (!founded)
            {
                Results.Add(result);
            }
        }

        private bool checkFunc(Result result, StringMatchFilter filter)
        {
            switch (filter.ContentPart.ToLower())
            {
                case "name":
                    return filter.Match(result.EntryName ?? "");
                case "content":
                    return filter.Match(result.Description ?? "");
                case "company":
                    return filter.Match(result.Company ?? "");
                default:
                    throw new Exception("Incorrect ContentPart: " + filter.ContentPart + ". Allows only \"Name\", \"Content\", \"Company\""); 
            }
        }

        private bool filter(Result result, ILockerList<StringMatchFilter> filters, IDictionary<StringMatchFilter, int> lastAcceptedFilters = null)
        {
            bool allow = false;
            lock (filters.Locker)
            {
                StringMatchFilter lastAcceptedFilter = null;
                foreach (var filter in filters)
                {
                    if (!filter.Enabled ||
                        allow == (filter.Permission == FilterPermission.Allow))
                    {
                        continue;
                    }

                    if (!filter.Negative == checkFunc(result, filter))
                    {
                        lastAcceptedFilter = filter;
                        allow = filter.Permission == FilterPermission.Allow;
                    }
                }
                if (lastAcceptedFilters != null && lastAcceptedFilter != null)
                {
                    int count = (lastAcceptedFilters.ContainsKey(lastAcceptedFilter)) ? lastAcceptedFilters[lastAcceptedFilter] : 0;
                    lastAcceptedFilters[lastAcceptedFilter] = count + 1;
                }
            }
            return allow;
        }

        public static string firstMatch(string text, string pattern, string replacement, bool htmlEncode)
        {
            if (String.IsNullOrEmpty(pattern)) return null;
            if (String.IsNullOrEmpty(text)) return null;
            Match match = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline).Match(text);
            if (!match.Success) return null;
            var result = String.IsNullOrEmpty(replacement) ? match.Value : match.Result(replacement);
            if (htmlEncode) result = HttpUtility.HtmlDecode(_removeTagsRegex.Replace(result, ""));
            return result;
        }

        private readonly int currentVersion = 1;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(currentVersion);
            lock (Sites.Locker) writer.WriteCollection(Sites, (w, o) => o.Serialize(w));
            lock (Results.Locker) writer.WriteCollection(Results, (w, o) => o.Serialize(w));
            lock (Favorites.Locker) writer.WriteCollection(Favorites, (w, o) => o.Serialize(w));
            lock (Favorites2.Locker) writer.WriteCollection(Favorites2, (w, o) => o.Serialize(w));
            lock (BlockedResults.Locker) writer.WriteCollection(BlockedResults, (w, o) => o.Serialize(w));
        }

        public object DeSerialize(BinaryReader reader)
        {
            int version = reader.ReadInt32();
            lock (Sites.Locker)
            {
                Sites.Clear();
                reader.ReadCollection(Sites, r => (Site) new Site().DeSerialize(r));
            }
            lock (Results.Locker)
            {
                Results.Clear();
                reader.ReadCollection(Results, r => (Result) new Result().DeSerialize(r));
            }
            lock (Favorites.Locker)
            {
                Favorites.Clear();
                reader.ReadCollection(Favorites, r => (Result) new Result().DeSerialize(r));
            }
            if (version > 0)
            {
                lock (Favorites2.Locker)
                {
                    Favorites2.Clear();
                    reader.ReadCollection(Favorites2, r => (Result)new Result().DeSerialize(r));
                }
                lock (BlockedResults.Locker)
                {
                    BlockedResults.Clear();
                    reader.ReadCollection(BlockedResults, r => (Result)new Result().DeSerialize(r));
                }
            }
            return this;
        }

        public event EventHandler Modified;

        public void OnModified(bool newThread)
        {
            if (Modified != null) Modified.Invoke(this, EventArgs.Empty);
        }

        private static bool AcceptAllCertifications(object sender, System.Security.Cryptography.X509Certificates.X509Certificate certification, System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {
            return true;
        } 
    }
}
