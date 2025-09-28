namespace EMSMVC.ViewModels
{
    public class UpdateUserRequest
    {
        public string? UserId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime? DOB { get; set; }
        public string? Phone { get; set; }
        public string? Gender { get; set; }
        public string? Address { get; set; }
        public string CurrentPassword { get; set; }
        //public string? ProfilePictureUrl { get; set; }
        //// الحقل الجديد
        //public IFormFile? ProfilePicture { get; set; }
    }

}
