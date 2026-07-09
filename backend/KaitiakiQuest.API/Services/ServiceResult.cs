namespace KaitiakiQuest.API.Services  
{
    public class ServiceResult<T>
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string>? Errors { get; set; }

        public static ServiceResult<T> Success(T data, string message = "Success")
        {
            return new ServiceResult<T> { IsSuccess = true, Message = message, Data = data };
        }

        public static ServiceResult<T> Failure(string message, List<string>? errors = null)
        {
            return new ServiceResult<T> { IsSuccess = false, Message = message, Errors = errors };
        }
    }
}