using EMSMVC.Models;

namespace EMSMVC.ViewModels
{
    public class RolePermissionsViewModel
    {
        public string RoleId { get; set; }
        public string RoleName { get; set; }
        public List<PermissionDTO>? CurrentPermissions { get; set; }
        public List<PermissionDTO>? AllPermissions { get; set; }
        public List<int>? SelectedPermissionIds { get; set; }
    }

}
