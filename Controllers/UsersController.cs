using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace UserApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _userRepository;

    public UsersController(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    // создание
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public ActionResult<User> CreateUser([FromBody] UserCreateDto dto)
    {
        if (_userRepository.GetByLogin(dto.Login) != null)
            return BadRequest("User with this login already exists");

        var currentUser = User.Identity?.Name ?? "System";
        var newUser = new User
        {
            Login = dto.Login,
            Password = dto.Password,
            Name = dto.Name,
            Gender = dto.Gender,
            Birthday = dto.Birthday,
            Admin = dto.Admin,
            CreatedBy = currentUser,
            ModifiedBy = currentUser
        };

        _userRepository.Add(newUser);
        return CreatedAtAction(nameof(CreateUser), newUser);
    }

    // изменение данных
    [HttpPut("{login}/details")]
    public IActionResult UpdateUserDetails(
        [FromRoute] string login,
        [FromBody] UserUpdateDetailsDto dto)
    {   
        if (string.IsNullOrEmpty(login))
            return BadRequest("Login is required");

        var currentUserLogin = User.Identity?.Name;
        if (currentUserLogin is null)
            return Unauthorized("User not authenticated");

        var targetUser = _userRepository.GetByLogin(login);
        if (targetUser is null)
            return NotFound("User not found");

        if (targetUser.RevokedOn is not null)
            return BadRequest("User is revoked");

        if (!User.IsInRole("Admin") && currentUserLogin != login)
            return Forbid();

        targetUser.Name = dto.Name;
        targetUser.Gender = dto.Gender;
        targetUser.Birthday = dto.Birthday;
        targetUser.ModifiedBy = currentUserLogin;
        targetUser.ModifiedOn = DateTime.UtcNow;

        _userRepository.Update(targetUser);
        return Ok(targetUser);
    }

    // изменение пароля
    [HttpPut("{login}/password")]
    public IActionResult ChangePassword(
        string login,
        [FromBody] ChangePasswordDto dto)
    {
        var currentUserLogin = User.Identity?.Name 
            ?? throw new InvalidOperationException("User not authenticated");

        if (string.IsNullOrEmpty(login))
            return BadRequest("Login is required");

        var targetUser = _userRepository.GetByLogin(login);
        if (targetUser is null)
            return NotFound("User not found");

        if (targetUser.RevokedOn is not null)
            return BadRequest("User is revoked");

        if (!User.IsInRole("Admin"))
        {
            if (string.IsNullOrEmpty(dto.OldPassword))
                return BadRequest("Old password is required");

            // сравниваем пароли (если используется хеширование)
            // if (!BCrypt.Net.BCrypt.Verify(dto.OldPassword, targetUser.Password))
            if (dto.OldPassword != targetUser.Password)
                return BadRequest("Invalid old password");
        }

        // обновление пароля (если используется хеширование)
        // targetUser.Password = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        targetUser.Password = dto.NewPassword;
        targetUser.ModifiedBy = currentUserLogin;
        targetUser.ModifiedOn = DateTime.UtcNow;

        _userRepository.Update(targetUser);
        return Ok("Password changed successfully");
    }

    // изменение логина
    [HttpPut("{login}/login")]
    public IActionResult UpdateLogin(
        [FromRoute] string login,
        [FromBody] UpdateLoginDto dto)
    {
        if (string.IsNullOrEmpty(login))
            return BadRequest("Login is required");

        var currentUserLogin = User.Identity?.Name;
        
        if (currentUserLogin is null)
            return Unauthorized();

        var targetUser = _userRepository.GetByLogin(login);
        if (targetUser is null) 
            return NotFound();

        if (targetUser.RevokedOn is not null)
            return BadRequest("User is revoked");

        if (_userRepository.GetByLogin(dto.NewLogin) is not null)
            return BadRequest("Login already exists");

        if (!User.IsInRole("Admin") && currentUserLogin != login)
            return Forbid();

        targetUser.Login = dto.NewLogin;
        targetUser.ModifiedBy = currentUserLogin;
        targetUser.ModifiedOn = DateTime.UtcNow;

        _userRepository.Update(targetUser);
        return Ok(targetUser);
    }

    // поиск по логину
    [HttpGet("{login}")]
    [Authorize(Roles = "Admin")]
    public IActionResult GetUserByLogin(
        [FromRoute] string login)
    {   
        if (string.IsNullOrWhiteSpace(login))
            return BadRequest("Login is required");

        var user = _userRepository.GetByLogin(login!);

        if (user is null)
            return NotFound();

        return Ok(new {
            user.Name,
            user.Gender,
            user.Birthday,
            IsActive = user.RevokedOn is null
        });
    }

    // текущий пользователь
    [HttpGet("me")]
    [Authorize]
    public IActionResult GetCurrentUser()
    {
        // получаем логин из токена
        var login = User.Identity?.Name;
        
        if (string.IsNullOrWhiteSpace(login))
            return Unauthorized("User not authenticated");

        // получаем пользователя из репозитория
        var user = _userRepository.GetByLogin(login!);

        // проверяем существование и активность
        if (user == null)
            return NotFound("User not found");
        
        if (user.RevokedOn != null)
            return Unauthorized("User account is revoked");

        return Ok(new {
            user.Name,
            user.Gender,
            user.Birthday,
            IsActive = user.IsActive
        });
    }

    // пользователи старше n-го возраста
    [HttpGet("older-than/{age}")]
    [Authorize(Roles = "Admin")]
    public IActionResult GetUsersOlderThan(int age)
    {
        if (age <= 0)
            return BadRequest("Age must be positive");

        var users = _userRepository.GetUsersOlderThan(age)
            ?? Enumerable.Empty<User>();

        return Ok(users);
    }

    // удаление пользователя
    [HttpDelete("{login}")]
    [Authorize(Roles = "Admin")]
    public IActionResult DeleteUser(
        [FromRoute] string login,
        [FromQuery] bool softDelete = true)
    {   
        if (string.IsNullOrEmpty(login))
            return BadRequest("Login is required");

        var user = _userRepository.GetByLogin(login);
        if (user == null) return NotFound();

        if (softDelete)
        {
            user.RevokedOn = DateTime.UtcNow;
            user.RevokedBy = User.Identity?.Name;
            _userRepository.Update(user);
        }
        else
        {
            _userRepository.Delete(user);
        }

        return NoContent();
    }

    // восстановление пользователя
    [HttpPost("{login}/restore")]
    [Authorize(Roles = "Admin")]
    public IActionResult RestoreUser(
        [FromRoute] string login)
    {   
        if (string.IsNullOrEmpty(login))
            return BadRequest("Login is required");

        var user = _userRepository.GetByLogin(login);
        if (user == null) return NotFound();

        user.RevokedOn = null;
        user.RevokedBy = null;
        _userRepository.Update(user);

        return Ok(user);
    }

    // список активных пользователей
    [HttpGet("active")]
    [Authorize(Roles = "Admin")]
    public IActionResult GetActiveUsers()
    {
        return Ok(_userRepository.GetAllActive());
    }
}

public class UserUpdateDetailsDto
{
    [Required]
    public required string Name { get; set; }

    [Range(0, 2)]
    public int Gender { get; set; }
    
    public DateTime? Birthday { get; set; }
}

public class ChangePasswordDto
{
    public string? OldPassword { get; set; }

    [Required(ErrorMessage = "New password is required")]
    [RegularExpression(@"^[a-zA-Z0-9_]+$", 
        ErrorMessage = "The password should contain only Latin letters, numbers and _")]
    public required string NewPassword { get; set; } 
}

public class UpdateLoginDto
{
    [Required(ErrorMessage = "New login is required")]
    [RegularExpression(@"^[a-zA-Z0-9_]+$", 
        ErrorMessage = "The login should contain only Latin letters, numbers and _")]
    public required string NewLogin { get; set; }
}