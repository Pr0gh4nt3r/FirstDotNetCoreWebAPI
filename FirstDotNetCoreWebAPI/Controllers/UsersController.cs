using FirstDotNetCoreWebAPI.Data;
using FirstDotNetCoreWebAPI.DTOs;
using FirstDotNetCoreWebAPI.Entities;
using FirstDotNetCoreWebAPI.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace FirstDotNetCoreWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController(DataContext dataContext) : ControllerBase
    {
        private readonly DataContext _dataContext = dataContext;

        [HttpGet("{email}")]
        public async Task<ActionResult<User>> GetUser(string email)
        {
            User? user = await _dataContext.Users.FindAsync(email);

            if (user == null)
                return NotFound("User not found.");

            return Ok(user);
        }

        [HttpPost]
        public async Task<ActionResult<User>> CreateUser(User user)
        {
            _dataContext.Users.Add(user);

            try
            {
                await _dataContext.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                bool userExists = DataCheckHelper.UserExistsAsync(_dataContext, user.Email).Result;

                if (userExists)
                    return BadRequest("A user with that email already exists.");
                else
                    throw;
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
                bool userExists = DataCheckHelper.UserExistsAsync(_dataContext, email).Result;

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
                bool userExists = DataCheckHelper.UserExistsAsync(_dataContext, email).Result;

                if (!userExists)
                    return NotFound("User not found.");
                else
                    throw;
            }

            return Ok("User successfully deleted.");
        }
    }
}
