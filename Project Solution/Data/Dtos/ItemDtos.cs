namespace UniShare.Data.Dtos
{
    public class ItemCreateDto
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Category { get; set; } // Changed from CategoryId (int) to Category (string)
        public decimal Price { get; set; }
        public string Condition { get; set; }
    }

    public class ItemUpdateDto
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Category { get; set; } // Changed from CategoryId (int?) to Category (string?)
        public decimal? Price { get; set; }
        public string? Condition { get; set; }
    }

    public class MessageCreateDto
    {
        public string Content { get; set; }
    }
}
