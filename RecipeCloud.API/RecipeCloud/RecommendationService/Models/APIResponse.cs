using System.Net;

namespace RecommendationService.Models
{
    public class APIResponse<T>
    {
        public T? Result { get; set; }
        public bool IsSuccess { get; set; }
        public List<string> ErrorsMessages { get; set; } = new();
        public HttpStatusCode StatusCode { get; set; }
    }

}
