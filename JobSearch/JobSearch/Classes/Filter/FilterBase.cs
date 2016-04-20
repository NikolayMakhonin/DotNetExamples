namespace JobSearch.Classes.Filter
{
    public abstract class FilterBase
    {
        public bool Enabled { get; set; }

        protected FilterBase(FilterPermission permission)
        {
            Permission = permission;
            Enabled = true;
        }

        public FilterPermission Permission { get; set; }

        public abstract bool Match(string text);
    }
}