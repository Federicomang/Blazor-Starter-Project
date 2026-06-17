using BlazorFeatures.Abstractions;

namespace StarterProject.Client.Extensions
{
    public static class PagedRequestExtensions
    {
        public static void FromTableState(this PagedRequest request, MudBlazor.TableState state)
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
            request.PageNumber = state.Page;
            request.PageSize = state.PageSize;
            request.OrderBy = orderBy;
        }
    }
}
