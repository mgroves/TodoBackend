using Microsoft.AspNetCore.Http;
using TodoBackend.Models;

namespace TodoBackend.ViewModels
{
    /// <summary>
    /// This class is meant to return the TodoItem to the public
    /// </summary>
    public class TodoItemView
    {
        public TodoItemView(TodoItem item, HttpRequest req)
        {
            Title = item.Title;
            Completed = item.Completed ?? false;
            Url = req.Scheme + "://" + req.Host + "/" + item.Id;
            Order = item.Order;
        }

        public string Title { get; }
        public bool Completed { get; }
        public string Url { get; }
        public int? Order { get; }
    }
}