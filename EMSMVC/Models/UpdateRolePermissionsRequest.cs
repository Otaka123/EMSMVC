namespace EMSMVC.Models
{
    public class UpdateRolePermissionsRequest
    {
        public string RoleId { get; set; }
        public List<int> SelectedPermissionIds { get; set; }
    }
}
