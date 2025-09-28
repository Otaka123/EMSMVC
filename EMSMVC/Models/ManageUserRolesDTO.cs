namespace EMSMVC.Models
{
    public class ManageUserRolesDTO
    {
        public string UserId { get; set; }

        public string? UserFullName { get; set; }

        public string? UserEmail { get; set; }

        public List<string> UserRoles { get; set; } = new List<string>();

        public List<string> SelectedRoles { get; set; } = new List<string>(); // تم التصحيح هنا

        public List<ApplicationRole> AllRoles { get; set; } = new List<ApplicationRole>(); // تم التصحيح
    }
}
