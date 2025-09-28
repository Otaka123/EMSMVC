namespace EMSMVC.Models
{
    public class ManageUserRolesRequest
    {
        public string UserId { get; set; }
        public List<string> SelectedRoleIds { get; set; }
    }

}
