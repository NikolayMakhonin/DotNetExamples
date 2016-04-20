namespace JobSearch.Classes.Filter
{
    public class Filter : StringMatchFilter
    {
        public Filter(string pattern, bool negative, string contentPart, FilterPermission permission, FilterSearchType searchType)
            : base(pattern, negative, contentPart, permission, searchType)
        {
        }
    }
}
