using System;
using System.IO;
using Logger.Utils;
using Utils;
using Utils.Serialization;

namespace JobSearch.Classes
{
    public class Site
    {
        [MemberOrder(0)]
        public bool Enabled { get; set; }
        [MemberOrder(10)]
        public string SiteName { get; set; }

        [MemberOrder(20)]
        public ILockerList<Request> Requests
        {
            get { return _requests; }
        }

        [MemberOrder(30)]
        public string EntryRegEx { get; set; }
        [MemberOrder(35)]
        public string EntryRegExResult { get; set; }
        [MemberOrder(40)]
        public string EntryNameRegEx { get; set; }
        [MemberOrder(45)]
        public string EntryNameRegExResult { get; set; }
        [MemberOrder(50)]
        public string UrlRegEx { get; set; }
        [MemberOrder(55)]
        public string UrlRegExResult { get; set; }
        [MemberOrder(60)]
        public string CompanyRegEx { get; set; }
        [MemberOrder(65)]
        public string CompanyRegExResult { get; set; }
        [MemberOrder(70)]
        public string DescriptionRegEx { get; set; }
        [MemberOrder(75)]
        public string DescriptionRegExResult { get; set; }
        [MemberOrder(80)]
        public string DateRegEx { get; set; }
        [MemberOrder(85)]
        public string DateRegExResult { get; set; }
        [MemberOrder(90)]
        public string CostRegEx { get; set; }
        [MemberOrder(95)]
        public string CostRegExResult { get; set; }
        [MemberOrder(100)]
        public string AnswersRegEx { get; set; }
        [MemberOrder(105)]
        public string AnswersRegExResult { get; set; }
        [MemberOrder(110)]
        public string Encoding { get; set; }

        private readonly int currentVersion = 3;
        private readonly ILockerList<Request> _requests = new SortedList<Request>(false, false);

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(currentVersion);
            writer.Write(Enabled);
            writer.WriteNullableString(SiteName);
            lock (Requests.Locker) writer.WriteCollection(Requests, (w, o) => o.Serialize(w));
            writer.WriteNullableString(EntryRegEx);
            writer.WriteNullableString(EntryRegExResult);
            writer.WriteNullableString(UrlRegEx);
            writer.WriteNullableString(UrlRegExResult);
            writer.WriteNullableString(DateRegEx);
            writer.WriteNullableString(DateRegExResult);
            writer.WriteNullableString(CompanyRegEx);
            writer.WriteNullableString(CompanyRegExResult);
            writer.WriteNullableString(EntryNameRegEx);
            writer.WriteNullableString(EntryNameRegExResult);
            writer.WriteNullableString(DescriptionRegEx);
            writer.WriteNullableString(DescriptionRegExResult);
            writer.WriteNullableString(Encoding);
            writer.WriteNullableString(CostRegEx);
            writer.WriteNullableString(CostRegExResult);
            writer.WriteNullableString(AnswersRegEx);
            writer.WriteNullableString(AnswersRegExResult);
        }

        public object DeSerialize(BinaryReader reader)
        {
            int version = reader.ReadInt32();
            Enabled = reader.ReadBoolean();
            SiteName = reader.ReadNullableString();
            lock (Requests.Locker)
            {
                Requests.Clear();
                if (version <= 1)
                {
                    var urls = reader.ReadNullableString();
                    foreach (var url in urls.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries))
                    {
                        Requests.Add(new Request {Url = url});
                    }
                }
                else
                {
                    reader.ReadCollection(Requests, r => (Request)new Request().DeSerialize(r));
                }
            }
            EntryRegEx = reader.ReadNullableString();
            EntryRegExResult = reader.ReadNullableString();
            UrlRegEx = reader.ReadNullableString();
            UrlRegExResult = reader.ReadNullableString();
            DateRegEx = reader.ReadNullableString();
            DateRegExResult = reader.ReadNullableString();
            CompanyRegEx = reader.ReadNullableString();
            CompanyRegExResult = reader.ReadNullableString();
            EntryNameRegEx = reader.ReadNullableString();
            EntryNameRegExResult = reader.ReadNullableString();
            DescriptionRegEx = reader.ReadNullableString();
            DescriptionRegExResult = reader.ReadNullableString();
            Encoding = reader.ReadNullableString();
            if (version > 0)
            {
                CostRegEx = reader.ReadNullableString();
                CostRegExResult = reader.ReadNullableString();
            }
            if (version > 2)
            {
                AnswersRegEx = reader.ReadNullableString();
                AnswersRegExResult = reader.ReadNullableString();
            }
            return this;
        }
    }
}
