using Agrovesto.Backend.API.Controllers;
using Agrovesto.Backend.Core.Interfaces.Managers;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace Agrovesto.Backend.Tests
{
    public class UnitTest1
    {
        private readonly Mock<IWalletProviderManager> _manager;
        private readonly Mock<IAuthenticationRepository> _authRepo;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly AdminController _controller;
        public UnitTest1()
        {
            _manager = new Mock<IWalletProviderManager>();
            _authRepo = new Mock<IAuthenticationRepository>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
            new Claim("sub", "test-user-id")
            }, "mock"));

            var httpContext = new DefaultHttpContext
            {
                User = user
            };

            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

           // _controller = new AdminController(_mockHttpContextAccessor.Object,_manager.Object, _authRepo.Object);
        }
        //[Fact]
        //public void GetProduct_ReturnsNotFound_WhenProductMissing()
        //{
        //    // Arrange
        //    _authRepo.Setup(s => s.GetUsersAsync()).Returns((Task<(UserModel model, bool status, string message)>)null);

        //    // Act
        //    var result = _controller.GetUsers();

        //    // Assert
        //    Assert.IsType<NotFoundResult>(result);
        //}
    }
}