namespace Light.ActiveDirectory.Dtos
{
    public record DomainUserDto(string UserName)
    {
        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }
    }
}