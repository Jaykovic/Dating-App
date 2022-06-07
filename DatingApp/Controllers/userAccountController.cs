using DatingApp.Data;
using DatingApp.DTOs;
using DatingApp.Entities;
using DatingApp.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DatingApp.Controllers
{
    public class userAccountController : BaseApiController
    {
        private readonly DataContext _context;

        private readonly ITokenService _tokenService;

        public userAccountController(DataContext context, ITokenService tokenService)
        {
            _tokenService = tokenService;
            _context = context;
        }

        // Post request to register users and has parameters
        [HttpPost("Register")]
       // public async Task<ActionResult<AppUser>> Register(RegisterDTO registerDTO)
        public async Task<ActionResult<UserDTO>> Register(RegisterDTO registerDTO)
        {
            // check if user exists
            if (await userExists(registerDTO.UserName)) return BadRequest("Username exists already, try again");
            
            // class to randomly generate key and dispose after use 
            using var hmac = new HMACSHA512();

            // create new user 
            var user = new AppUser
            {
                UserName = registerDTO.UserName.ToLower(),
                passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDTO.Password)),
                passwordSalt = hmac.Key
            };

            // Add user
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return new UserDTO
            {
                UserName = user.UserName,
                Token = _tokenService.createToken(user)
            };
        }

        // Login method
        [HttpPost("Login")]

        public async Task<ActionResult<UserDTO>> Login(LoginDTO loginDTO)
        {
            // find a particular user
            var user = await _context.Users.SingleOrDefaultAsync(x => x.UserName == loginDTO.UserName);

            // check is user exists
            if (user == null) return Unauthorized("Invalid username");

            // check for password
            using var hmac = new HMACSHA512(user.passwordSalt);
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDTO.Password));

            for(int i = 0; i < computedHash.Length; i++)
            {
                if (computedHash[i] != user.passwordHash[i]) return Unauthorized("Invalid Password");
            }

            return new UserDTO
            {
                UserName = user.UserName,
                Token = _tokenService.createToken(user)
            };
        }

        // method to check if user exists in the system
        private async Task<bool> userExists(string username)
        {
            return await _context.Users.AnyAsync(x => x.UserName == username.ToLower());
        }
    }
}
