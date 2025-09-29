using Microsoft.AspNetCore.Mvc;
using Moq;
using TipBuddyApi.Controllers;
using TipBuddyApi.Contracts;

namespace TipBuddyApi.Tests.Controllers
{
    public class DemoDataControllerTests
    {
        private readonly Mock<IDemoDataSeeder> _demoDataSeederMock;
        private readonly DemoDataController _controller;

        public DemoDataControllerTests()
        {
            _demoDataSeederMock = new Mock<IDemoDataSeeder>();
            _controller = new DemoDataController(_demoDataSeederMock.Object);
        }

        [Fact]
        public async Task ResetDemoData_ReturnsOk()
        {
            // Arrange
            _demoDataSeederMock.Setup(s => s.ResetDemoUserAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.ResetDemoData();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            Assert.Contains("Demo data has been reset.", okResult.Value.ToString());
            _demoDataSeederMock.Verify(s => s.ResetDemoUserAsync(), Times.Once);
        }
    }
}
