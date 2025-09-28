using System.Security;

namespace EMSMVC.Models
{
    public class PermissionDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string PermissionType { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // يمكن إضافة خصائص إضافية إذا لزم الأمر
        public string DisplayName
        {
            get
            {
                // إذا كان الاسم يحتوي على نقطة، نأخذ الجزء بعد النقطة
                if (!string.IsNullOrEmpty(Name) && Name.Contains('.'))
                {
                    return Name.Split('.')[1];
                }
                return Name;
            }
        }
    }
}


