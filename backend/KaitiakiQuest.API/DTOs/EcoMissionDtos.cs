namespace KaitiakiQuest.API.DTOs
{
    // a request when creating task
    public class CreateEcoMissionDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int BasePoints { get; set; }
        public string Category { get; set; } = "Recycling";
        public string? ImageUrl { get; set; }
        public bool IsDaily { get; set; } = false;
    }

    // a request when updating task
    public class UpdateEcoMissionDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int BasePoints { get; set; }
        public string Category { get; set; } = "Recycling";
        public string? ImageUrl { get; set; }
        public bool IsDaily { get; set; } = false;
        public bool IsActive { get; set; } = true;
    }

    // Return the response when the task is completed
    public class EcoMissionResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int BasePoints { get; set; }
        public string Category { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public bool IsDaily { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}