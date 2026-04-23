using WebHS.Models;

namespace WebHS.ViewModels
{
    public class MessageTemplateViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public MessageTemplateType Type { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class MessageTemplateListViewModel
    {
        public List<MessageTemplateViewModel> Templates { get; set; } = new List<MessageTemplateViewModel>();
        public string SearchTerm { get; set; } = string.Empty;
        public MessageTemplateType? SelectedType { get; set; }
    }

    public class CreateMessageTemplateViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public MessageTemplateType Type { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class BulkToggleRequest
    {
        public int[] Ids { get; set; } = Array.Empty<int>();
        public bool IsActive { get; set; }
    }

    public class MessageTemplateStatsViewModel
    {
        public int Total { get; set; }
        public int Active { get; set; }
        public int Inactive { get; set; }
        public List<MessageTemplateTypeStats> ByType { get; set; } = new List<MessageTemplateTypeStats>();
    }

    public class MessageTemplateTypeStats
    {
        public MessageTemplateType Type { get; set; }
        public int Count { get; set; }
    }
}
