namespace TodoBackend.Models
{
    /// <summary>
    /// This class is meant to model the TodoItem for the database.
    /// The Id value is NOT serialized within the document, it's only stored
    /// as the document key
    /// </summary>
    public class TodoItem
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public bool? Completed { get; set; }
        public int? Order { get; set; }

        public bool ShouldSerializeId() => false;
        public bool ShouldDeserializeId() => true;
    }
}