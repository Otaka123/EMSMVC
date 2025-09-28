using Common.Application.Common;

namespace EMSMVC.Models
{
    public class PermissionApiResponse
    {
        public RolePermissionsData Data { get; set; }
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public ResponseError ErrorR { get; set; }
        public List<string> Errors { get; set; }
    }
}
