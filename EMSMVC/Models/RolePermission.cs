using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security;

namespace EMSMVC.Models
{
    public class RolePermission
    {
        [Key, Column(Order = 0)]
        public string RoleId { get; set; }

        [Key, Column(Order = 1)]
        public int PermissionId { get; set; }

        // Navigation
        [ForeignKey(nameof(RoleId))]
        public ApplicationRole Role { get; set; }

        [ForeignKey(nameof(PermissionId))]
        public Permission Permission { get; set; }

        // ✅ Constructor فارغ مطلوب لـ EF Core
        public RolePermission() { }

        // ✅ Constructor مخصص لتسهيل الإنشاء
        public RolePermission(string roleId, int permissionId)
        {
            RoleId = roleId;
            PermissionId = permissionId;
        }
    }
}
