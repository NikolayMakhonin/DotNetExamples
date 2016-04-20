using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace JobSearch.Classes
{
    public abstract class RequestDownloadFuncBase
    {
        protected abstract IEnumerable<string> func(Request request);

        public IEnumerable<string> Func(Request request)
        {           
            var result = func(request);
            return result;
        }

        protected string download(string url, Encoding encoding)
        {
            return JobSearcher.Download(new Request { Url = url, Encoding = encoding });
        }

        protected string download(string url, string postData, Encoding encoding)
        {
            return JobSearcher.Download(new Request { Url = url, PostData = postData, Encoding = encoding });
        }

        protected string download(Request request)
        {
            return JobSearcher.Download(request);
        }

        protected string download(string url)
        {
            return download(url, Encoding.UTF8);
        }

        protected string download(string url, string postData)
        {
            return download(url, postData, Encoding.UTF8);
        }

        protected string match(string text, string pattern, string replacement = null, bool htmlEncode = false)
        {
            return JobSearcher.firstMatch(text, pattern, replacement, htmlEncode);
        }
    }
}
