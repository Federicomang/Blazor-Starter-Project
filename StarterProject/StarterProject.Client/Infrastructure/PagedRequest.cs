namespace StarterProject.Client.Infrastructure
{
    public interface IPagedRequest
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public string? OrderBy { get; set; }
    }

    public class PagedRequest : IPagedRequest
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public string? OrderBy { get; set; }

        public void FromTableState(MudBlazor.TableState state)
        {
            string? orderBy = null;
            string sortDir = string.Empty;
            switch (state.SortDirection)
            {
                case MudBlazor.SortDirection.Ascending:
                    sortDir = " asc";
                    break;
                case MudBlazor.SortDirection.Descending:
                    sortDir = " desc";
                    break;
            }
            if (!string.IsNullOrEmpty(state.SortLabel))
            {
                orderBy = state.SortLabel + sortDir;
            }
            PageNumber = state.Page;
            PageSize = state.PageSize;
            OrderBy = orderBy;
        }
    }
}
