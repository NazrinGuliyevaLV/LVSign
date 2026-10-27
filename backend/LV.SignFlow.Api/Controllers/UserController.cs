using LV.SignFlow.Application.Common.Interfaces;
using LV.SignFlow.Application.Users.Dtos;
using LV.SignFlow.Domain.Entities.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LV.SignFlow.Api.Controllers
{
    [Route("api/users")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IRepository<User> _userRepository;
        private readonly ICurrentUser _currentUser;

        public UserController(IRepository<User> userRepository, ICurrentUser currentUser)
        {
            _userRepository = userRepository;
            _currentUser = currentUser;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<UserLookupDto>>> Get(
        CancellationToken cancellationToken)
        {
            if (!_currentUser.IsAuthentificated)
                return Unauthorized();

            var users =await _userRepository.FindAsync(
                    x =>x.OrganizationId == _currentUser.OrganizationId && x.IsActive,cancellationToken);

            var result =
                users.OrderBy(x => x.DisplayName)
                    .Select(x => new UserLookupDto
                    {
                        Id = x.Id,
                        DisplayName = x.DisplayName,
                        Email = x.Email,
                        JobTitle = x.JobTitle
                    })
                    .ToList();

            return Ok(result);
        }
    }
}

