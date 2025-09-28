using Microsoft.AspNetCore.Identity;

namespace EMSMVC.Models
{
    public class ApplicationRole : IdentityRole
    {
        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
        public ApplicationRole() { } // Default constructor

        public ApplicationRole(string roleName) : base(roleName)
        {
        }
    }
}
