namespace TipBuddyApi.Dtos.Auth
{
    public class RegisterDto
    {
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public int? PhoneNumber { get; set; }
        public required string UserName { get; set; }
        public required string Email { get; set; }

        public required string Password { get; set; }
    }
}