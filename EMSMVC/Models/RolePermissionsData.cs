namespace EMSMVC.Models
{
    public class RolePermissionsData
    {
        public string RoleId { get; set; }
        public string RoleName { get; set; }
        public List<PermissionDTO> CurrentPermissions { get; set; }
        public List<PermissionDTO> AllPermissions { get; set; }
    }
}
