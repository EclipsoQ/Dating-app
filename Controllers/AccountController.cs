using System.Security.Cryptography;
using System.Text;
using API;
using API.Entities;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API
{
    public class AccountController(DataContext context, ITokenService tokenService
        , IMapper mapper) : BaseApiController
    {
        [HttpPost("register")] // account/register
        public async Task<ActionResult<UserDto>> Register (RegisterDto registerDto) 
        {
            if (await UserExists(registerDto.Username)) return BadRequest("Username is taken");

            using var hmac = new HMACSHA512();
           
            var user = mapper.Map<AppUser>(registerDto);
            user.PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.Password));
            user.PasswordSalt = hmac.Key;             

            context.Users.Add(user);
            await context.SaveChangesAsync();
            
            return new UserDto 
            {
                Username = user.UserName,
                Token = tokenService.CreateToken(user),
                KnownAs = user.KnownAs,
                Gender = user.Gender,                   
            };
        }

        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login(LoginDto loginDto) 
        {
            var user = await context.Users.Include(u => u.Photos)
                .FirstOrDefaultAsync(u => u.UserName == loginDto.Username.ToLower());
            if (user == null) return Unauthorized("Invalid username");

            using var hmac = new HMACSHA512(user.PasswordSalt);
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));

            for (int i = 0; i < computedHash.Length; i++) 
            {
                if (computedHash[i] != user.PasswordHash[i]) return Unauthorized("Invalid password");
            }
            return new UserDto 
            {
                Username = user.UserName,
                KnownAs = user.KnownAs,
                Token = tokenService.CreateToken(user),
                Gender = user.Gender,
                PhotoUrl = user.Photos.FirstOrDefault(x => x.IsMain)?.Url
            };
        }

        private async Task<bool> UserExists(string username) 
        {
            return await context.Users.AnyAsync(u => u.UserName.ToLower() == username.ToLower());
        }
        
    }
}
