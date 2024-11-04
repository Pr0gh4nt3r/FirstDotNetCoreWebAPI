using FirstDotNetCoreWebAPI.Data;
using FirstDotNetCoreWebAPI.DTOs;
using FirstDotNetCoreWebAPI.Entities;
using FirstDotNetCoreWebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace FirstDotNetCoreWebAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController(DataContext dataContext, UserService userService) : ControllerBase
    {
        private readonly DataContext _dataContext = dataContext;
        private readonly UserService _userService = userService;

        [AllowAnonymous]
        [HttpGet("{email}")]
        public async Task<ActionResult<User>> GetUser(string email)
        {
            User? user = await _dataContext.Users.FindAsync(email);

            if (user == null)
                return NotFound("User not found.");

            return Ok(user);
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<ActionResult<User>> CreateUser(User _user)
        {
            User? user = await _userService.RegisterUserAsync(_user.Email, _user.Password);

            if (user == null)
            {
                bool userExists = await _userService.UserExistsAsync(_user.Email);

                if (userExists)
                    return BadRequest("A user with that email already exists.");

                return BadRequest("Something went wrong while register the user.");
            }

            return Ok(user);
        }

        [HttpPatch("{email}")]
        public async Task<ActionResult<User>> UpdateUser(string email, UserUpdateDTO userDTO)
        {
            if (userDTO == null)
                return BadRequest();

            User? user = await _dataContext.Users.FindAsync(email);

            if (user == null)
                return NotFound("User not found.");

            if (userDTO.Email != null && userDTO.Email != user.Email)
                user.Email = userDTO.Email;

            if (userDTO.Password != null  && userDTO.Password != user.Password)
                user.Password = userDTO.Password;

            if (userDTO.IsEmailConfirmed == true)
                user.IsEmailConfirmed = true;

            try
            {
                await _dataContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                bool userExists = _userService.UserExistsAsync(email).Result;

                if (!userExists)
                    return NotFound("User not found.");
                else
                    throw;
            }

            return Ok(user);
        }

        [HttpDelete("{email}")]
        public async Task<ActionResult<User>> DeleteUser(string email)
        {
            User? user = await _dataContext.Users.FindAsync(email);

            if (user == null)
                return NotFound("User not found.");

            _dataContext.Users.Remove(user);

            try
            {
                await _dataContext.SaveChangesAsync();
            }
            catch (DBConcurrencyException)
            {
                bool userExists = _userService.UserExistsAsync(email).Result;

                if (!userExists)
                    return NotFound("User not found.");
                else
                    throw;
            }

            return Ok("User successfully deleted.");
        }
    }
}
