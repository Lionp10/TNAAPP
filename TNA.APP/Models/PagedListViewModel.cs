using System.Collections.Generic;

namespace TNA.APP.Models
{
    public class PagedListViewModel<T>
    {
        public List<T> Items { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }

        public PagedListViewModel() { }

        public PagedListViewModel(IEnumerable<T> items, int page, int pageSize, int totalItems)
        {
            Items = new List<T>(items ?? new List<T>());
            Page = page < 1 ? 1 : page;
            PageSize = pageSize < 1 ? 10 : pageSize;
            TotalItems = totalItems;
            TotalPages = (int)System.Math.Ceiling(TotalItems / (double)PageSize);
        }
    }
}
