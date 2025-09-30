using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using TipBuddyApi.Contracts;
using TipBuddyApi.Controllers;
using TipBuddyApi.Data;
using TipBuddyApi.Dtos.Shift;

namespace TipBuddyApi.Tests.Controllers
{
    public class ShiftsControllerTests
    {
        private readonly Mock<IShiftsRepository> _repoMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly ShiftsController _controller;

        public ShiftsControllerTests()
        {
            _repoMock = new Mock<IShiftsRepository>();
            _mapperMock = new Mock<IMapper>();
            _controller = new ShiftsController(_repoMock.Object, _mapperMock.Object);
        }

        private void SetUser(params Claim[] claims)
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuthType"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };        
        }

        [Fact]
        public async Task GetShifts_ReturnsUnauthorized_IfNoUserId()
        {
            var result = await _controller.GetShifts();
            Assert.IsType<UnauthorizedResult>(result.Result);
        }

        [Fact]
        public async Task GetShifts_UsesNameIdentifierClaim_WhenAvailable()
        {
            var userId = "user1";
            SetUser(new Claim(ClaimTypes.NameIdentifier, userId));
            var shifts = new List<Shift> { new Shift { Id = "1", UserId = userId } };
            var dtos = new List<GetShiftDto> { new GetShiftDto { Id = "1" } };

            _repoMock.Setup(r => r.GetShiftsAsync(userId, null, null)).ReturnsAsync(shifts);
            _mapperMock.Setup(m => m.Map<List<GetShiftDto>>(shifts)).Returns(dtos);

            var result = await _controller.GetShifts();
            Assert.Equal(dtos, result.Value);
        }

        [Fact]
        public async Task GetShifts_FallsBackToSubClaim_WhenNameIdentifierMissing()
        {
            var userId = "user-sub";
            SetUser(new Claim(JwtRegisteredClaimNames.Sub, userId));
            var shifts = new List<Shift> { new Shift { Id = "1", UserId = userId } };
            var dtos = new List<GetShiftDto> { new GetShiftDto { Id = "1" } };

            _repoMock.Setup(r => r.GetShiftsAsync(userId, null, null)).ReturnsAsync(shifts);
            _mapperMock.Setup(m => m.Map<List<GetShiftDto>>(shifts)).Returns(dtos);

            var result = await _controller.GetShifts();
            Assert.Equal(dtos, result.Value);
        }

        [Fact]
        public async Task GetShift_ReturnsNotFound_IfMissing()
        {
            _repoMock.Setup(r => r.GetAsync("1")).ReturnsAsync((Shift)null);
            var result = await _controller.GetShift("1");
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task GetShift_ReturnsShift_IfExists()
        {
            var shift = new Shift { Id = "1", UserId = "userId" };
            _repoMock.Setup(r => r.GetAsync("1")).ReturnsAsync(shift);
            var result = await _controller.GetShift("1");
            Assert.Equal(shift, result.Value);
        }

        [Fact]
        public async Task GetShifts_ReturnsUnauthorized_IfUserIsNull()
        {
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = null }
            };
            var result = await _controller.GetShifts();
            Assert.IsType<UnauthorizedResult>(result.Result);
        }

        [Fact]
        public async Task GetShifts_ReturnsUnauthorized_IfUserHasNoClaims()
        {
            SetUser();
            var result = await _controller.GetShifts();
            Assert.IsType<UnauthorizedResult>(result.Result);
        }

        [Fact]
        public async Task PutShift_ReturnsBadRequest_IfIdMismatch()
        {
            var dto = new UpdateShiftDto { Id = "2" };
            var result = await _controller.PutShift("1", dto);
            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task PutShift_ReturnsNotFound_IfShiftMissing()
        {
            var dto = new UpdateShiftDto { Id = "1" };
            _repoMock.Setup(r => r.GetAsync("1")).ReturnsAsync((Shift)null);
            var result = await _controller.PutShift("1", dto);
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task PutShift_ReturnsNoContent_IfSuccess()
        {
            var dto = new UpdateShiftDto { Id = "1" };
            var shift = new Shift { Id = "1", UserId = "userId" };
            _repoMock.Setup(r => r.GetAsync("1")).ReturnsAsync(shift);
            _repoMock.Setup(r => r.UpdateAsync(shift)).Returns(Task.CompletedTask);
            var result = await _controller.PutShift("1", dto);
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task PostShift_ReturnsCreatedAtAction()
        {
            var userId = "user1";
            SetUser(new Claim(ClaimTypes.NameIdentifier, userId));

            var dto = new CreateShiftDto();
            var shift = new Shift { Id = "1", UserId = userId };
            var getDto = new GetShiftDto { Id = "1" };

            _mapperMock.Setup(m => m.Map<Shift>(dto)).Returns(shift);
            _repoMock.Setup(r => r.AddAsync(shift)).Returns(Task.FromResult(shift));
            _mapperMock.Setup(m => m.Map<GetShiftDto>(shift)).Returns(getDto);

            var result = await _controller.PostShift(dto);
            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(getDto, created.Value);
        }

        [Fact]
        public async Task DeleteShift_ReturnsNotFound_IfMissing()
        {
            _repoMock.Setup(r => r.Exists("1")).ReturnsAsync(false);
            var result = await _controller.DeleteShift("1");
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeleteShift_ReturnsNoContent_IfExists()
        {
            _repoMock.Setup(r => r.Exists("1")).ReturnsAsync(true);
            _repoMock.Setup(r => r.DeleteAsync("1")).Returns(Task.CompletedTask);

            var result = await _controller.DeleteShift("1");
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task PutShift_ReturnsNotFound_WhenDbUpdateConcurrencyException_AndShiftDoesNotExist()
        {
            var dto = new UpdateShiftDto { Id = "1" };
            var shift = new Shift { Id = "1", UserId = "userId" };
            _repoMock.Setup(r => r.GetAsync("1")).ReturnsAsync(shift);
            _repoMock.Setup(r => r.UpdateAsync(shift)).ThrowsAsync(new Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException());
            _repoMock.Setup(r => r.Exists("1")).ReturnsAsync(false);

            var result = await _controller.PutShift("1", dto);
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task PutShift_Throws_WhenDbUpdateConcurrencyException_AndShiftExists()
        {
            var dto = new UpdateShiftDto { Id = "1" };
            var shift = new Shift { Id = "1", UserId = "userId" };
            _repoMock.Setup(r => r.GetAsync("1")).ReturnsAsync(shift);
            _repoMock.Setup(r => r.UpdateAsync(shift)).ThrowsAsync(new Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException());
            _repoMock.Setup(r => r.Exists("1")).ReturnsAsync(true);

            await Assert.ThrowsAsync<Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException>(() => _controller.PutShift("1", dto));
        }
    }
}
