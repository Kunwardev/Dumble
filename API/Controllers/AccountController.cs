using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    public class AccountController: BaseAPIController
    {
        private readonly DataContext _context;
        private readonly ITokenService _tokenservice;
        public AccountController(DataContext context, ITokenService tokenService)
        {
            _context = context;
            _tokenservice = tokenService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<UserDTO>> Register(RegisterDTO register)
        {
            if(await UserExists(register.UserName)) return new BadRequestObjectResult("Username is taken");

            using var hmac = new HMACSHA512();
            var user = new AppUser
            {
                UserName = register.UserName.ToLower(),
                PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(register.Password)),
                PasswordSalt = hmac.Key
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return new UserDTO
            {
                Username = user.UserName,
                Token = _tokenservice.CreateToken(user)
            };
        }

        [HttpPost("login")]
        public async Task<ActionResult<UserDTO>> Login(LoginDTO loginDTO)
        {
            var user = await _context.Users.SingleOrDefaultAsync(x => x.UserName == loginDTO.userName);
            if(user == null)
                return new BadRequestObjectResult("Invalid UserName");
            
            using var hmac = new HMACSHA512(user.PasswordSalt);
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDTO.password));

            for(int i=0; i <computedHash.Length; i++ )
            {
                if(computedHash[i] != user.PasswordHash[i])
                    return new BadRequestObjectResult("Invalid Password");
            }
            return new UserDTO
            {
                Username = user.UserName,
                Token = _tokenservice.CreateToken(user)
            };
        }

        private async Task<bool> UserExists(string username)
        {
            return await _context.Users.AnyAsync(x => x.UserName == username.ToLower());
        }

    }
}