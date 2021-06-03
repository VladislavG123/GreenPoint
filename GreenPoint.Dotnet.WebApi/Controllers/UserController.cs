using System;
using System.Threading.Tasks;
using GreenPoint.Dotnet.Contracts.Parameters;
using GreenPoint.Dotnet.Contracts.ViewModels;
using GreenPoint.Dotnet.DataAccess.Providers;
using GreenPoint.Dotnet.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace GreenPoint.Dotnet.WebApi.Controllers
{
    [Authorize]
    [Route("user")]
    public class UserController : ControllerBase
    {
        private readonly UserProvider _userProvider;
        private readonly AvatarProvider _avatarProvider;
        private readonly StatusProvider _statusProvider;
        private readonly UserAuthenticationService _authenticationService;

        public UserController(UserProvider userProvider, UserAuthenticationService authenticationService, 
            AvatarProvider avatarProvider, StatusProvider statusProvider)
        {
            _userProvider = userProvider;
            _authenticationService = authenticationService;
            _avatarProvider = avatarProvider;
            _statusProvider = statusProvider;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var user = await _authenticationService
                .GetUserByHeaders(Request.Headers[HeaderNames.Authorization].ToArray());
            var avatar = await _avatarProvider.GetById(user.AvatarId);

            var userRating = await _userProvider.CountRating(user.Id);
            
            var status = await _statusProvider.GetByRating(userRating);
            
            return Ok(new UserViewModel
            {
                Email = user.Email,
                Id = user.Id,
                Username = user.Username,
                Status = status.Name,
                AvatarUrl = avatar?.Url
            });

        }
        
        [HttpPatch]
        public async Task<IActionResult> Patch([FromBody] PatchUserParameter parameter)
        {
            var user = await _authenticationService
                .GetUserByHeaders(Request.Headers[HeaderNames.Authorization].ToArray());
            
            if(!string.IsNullOrEmpty(parameter.Username))
            {
                user.Username = parameter.Username;
            }
            
            if(parameter.AvatarId != Guid.Empty)
            {
                user.AvatarId = parameter.AvatarId;
            }
            
            if(!string.IsNullOrEmpty(parameter.Password))
            {
                user.Password = BCrypt.Net.BCrypt.HashPassword(parameter.Password);
            }
            
            await _userProvider.Edit(user);
            return NoContent();
        }
    }
}