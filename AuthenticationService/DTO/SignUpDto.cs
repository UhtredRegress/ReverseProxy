namespace AuthenticationService.DTO;

public record SignUpDto(string Email, string Password, string ConfirmPassword);