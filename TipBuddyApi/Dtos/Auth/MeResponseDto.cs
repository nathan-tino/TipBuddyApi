using TipBuddyApi.Data;

namespace TipBuddyApi.Dtos.Auth
{
    public class MeResponseDto
    {
        public string Id { get; set; } = string.Empty;
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public IEnumerable<string> Roles { get; set; } = [];
        public DateTimeOffset? IssuedAt { get; set; }
        public DateTimeOffset? ExpiresAt { get; set; }
        public bool IsDemo { get; set; }

        public MeResponseDto(User user, IEnumerable<string> roles, DateTimeOffset? issuedAt, DateTimeOffset? expiresAt, bool isDemo)
        {
            Id = user.Id;
            UserName = user.UserName;
            Email = user.Email;
            Roles = roles;
            IssuedAt = issuedAt;
            ExpiresAt = expiresAt;
            IsDemo = isDemo;
        }
    }
}