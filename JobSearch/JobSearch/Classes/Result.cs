using System;
using System.IO;
using Utils;
using Utils.Contracts.Patterns;
using Utils.Serialization;

namespace JobSearch.Classes
{
    public class Result : IStreamSerializable, IComparable
    {
        [MemberOrder(0)]
        public bool Selected { get; set; }
        [MemberOrder(5)]
        public double Priority { get; set; }

        [MemberOrder(10)]
        public string SiteName
        {
            get { return _siteName; }
            set
            {
                _siteName = value;
            }
        }

        [MemberOrder(20)]
        public string Company { get; set; }
        [MemberOrder(30)]
        public string EntryName { get; set; }
        [MemberOrder(40)]
        public string Url { get; set; }
        [MemberOrder(50)]
        public string Date { get; set; }
        [MemberOrder(60)]
        public string Description { get; set; }
        [MemberOrder(70)]
        public string Cost { get; set; }
        [MemberOrder(80)]
        public string Answers { get; set; }

        private readonly int currentVersion = 2;
        private string _siteName;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(currentVersion);
            writer.Write(Selected);
            writer.WriteNullableString(SiteName);
            writer.WriteNullableString(Url);
            writer.WriteNullableString(Date);
            writer.WriteNullableString(Company);
            writer.WriteNullableString(EntryName);
            writer.WriteNullableString(Description);
            writer.WriteNullableString(Cost);
            writer.WriteNullableString(Answers);
            writer.Write(Priority);
        }

        public object DeSerialize(BinaryReader reader)
        {
            int version = reader.ReadInt32();
            Selected = reader.ReadBoolean();
            SiteName = reader.ReadNullableString();
            Url = reader.ReadNullableString();
            Date = reader.ReadNullableString();
            Company = reader.ReadNullableString();
            EntryName = reader.ReadNullableString();
            Description = reader.ReadNullableString();
            Cost = reader.ReadNullableString();
            if (version > 0)
            {
                Answers = reader.ReadNullableString();
            }
            if (version > 1)
            {
                Priority = reader.ReadDouble();
            }
            return this;
        }

        public int CompareTo(object obj)
        {
            var typedObj = obj as Result;
            if (typedObj == null) return -1;
            if (typedObj.Url == null)
            {
                if (Url == null) return GetHashCode().CompareTo(typedObj.GetHashCode());
                return -1;
            }
            if (Url == null) return 1;
            return String.Compare(Url, typedObj.Url, StringComparison.OrdinalIgnoreCase);
        }
    }
}
