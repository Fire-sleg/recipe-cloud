using System.Net;

namespace RecipeService.Models
{
    public class APIResponse<T>
    {
        public HttpStatusCode StatusCode { get; set; }
        public bool IsSuccess { get; set; } = true;
        public List<string> ErrorMessages { get; set; } = new List<string>();
        public T? Result { get; set; }

        
        public APIResponse() { }

        
        public static APIResponse<T> Success(T result, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            return new APIResponse<T>
            {
                Result = result,
                StatusCode = statusCode,
                IsSuccess = true
            };
        }

        public static APIResponse<T> Fail(string errorMessage, HttpStatusCode statusCode = HttpStatusCode.BadRequest)
        {
            return new APIResponse<T>
            {
                StatusCode = statusCode,
                IsSuccess = false,
                ErrorMessages = new List<string> { errorMessage }
            };
        }

        public static APIResponse<T> Fail(List<string> errorMessages, HttpStatusCode statusCode = HttpStatusCode.BadRequest)
        {
            return new APIResponse<T>
            {
                StatusCode = statusCode,
                IsSuccess = false,
                ErrorMessages = errorMessages
            };
        }
    }

}
