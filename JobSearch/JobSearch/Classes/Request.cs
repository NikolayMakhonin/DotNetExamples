using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UniversalEditControls.Controls.EditControls.ObjectEdit;
using Utils;
using Utils.Contracts.Patterns;
using Utils.Serialization;

namespace JobSearch.Classes
{
    public enum RequestType
    {
        GET, POST
    }

    public class Request : IStreamSerializable, ICloneable
    {
        [MemberOrder(0)]
        public bool Enabled { get; set; }
        [MemberOrder(10)]
        public string Url { get; set; }
        [MemberOrder(20)]
        public string PostData { get; set; }
        [MemberOrder(30)]
        public RequestType RequestType { get; set; }
        [MemberOrder(40)]
        public string Referer { get; set; }
        [MemberOrder(50)]
        public string Origin { get; set; }

        private string _downloadFuncCode;
        [MemberOrder(60)]
        public string DownloadFuncCode
        {
            get { return _downloadFuncCode; }
            set
            {
                DownloadFunc = null;
                _downloadFuncCode = value;
            }
        }

        [ObjectEditorFilter(HideFromList = true, HideFromTable = true)]
        public Encoding Encoding { get; set; }
        [ObjectEditorFilter(HideFromList = true, HideFromTable = true)]
        public Func<Request, IEnumerable<string>> DownloadFunc { get; set; }

        private readonly int currentVersion = 6;

        public Request() : this(null) { }
        public Request(Request source)
        {
            Encoding = Encoding.UTF8;
            Enabled = true;
            if (source != null)
            {
                RequestType = source.RequestType;
                Enabled = source.Enabled;
                Url = source.Url;
                PostData = source.PostData;
                Encoding = source.Encoding;
                Referer = source.Referer;
                Origin = source.Origin;
                DownloadFuncCode = source.DownloadFuncCode;
                DownloadFunc = source.DownloadFunc;
            }
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(currentVersion);
            writer.Write(Enabled);
            writer.Write((int)RequestType);
            writer.WriteNullableString(Url);
            writer.WriteNullableString(PostData);
            writer.WriteNullableString(DownloadFuncCode);
            writer.WriteNullableString(Referer);
            writer.WriteNullableString(Origin);
        }

        public object DeSerialize(BinaryReader reader)
        {
            int version = reader.ReadInt32();
            if (version > 2)
            {
                Enabled = reader.ReadBoolean();
            }
            RequestType = (RequestType)reader.ReadInt32();
            Url = reader.ReadNullableString();
            PostData = reader.ReadNullableString();
            if (version > 3) DownloadFuncCode = reader.ReadNullableString();
            if (version > 4) Referer = reader.ReadNullableString();
            if (version > 5) Origin = reader.ReadNullableString();
            return this;
        }

        public object Clone()
        {
            return new Request(this);
        }
    }
}