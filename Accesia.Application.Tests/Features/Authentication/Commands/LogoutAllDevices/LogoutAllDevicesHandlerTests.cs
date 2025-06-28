using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Accesia.Application.Features.Authentication.Commands.LogoutAllDevices;
using Accesia.Application.Features.Authentication.DTOs;
using Accesia.Application.Common.Interfaces;
using Accesia.Domain.Entities;
using Accesia.Domain.ValueObjects;
using Accesia.Domain.Enums;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Linq; // Required for CountAsync
using System.Linq.Expressions;
using System.Collections.Generic; // Required for List
using Microsoft.EntityFrameworkCore; // Required for async operations like CountAsync


namespace Accesia.Application.Tests.Features.Authentication.Commands.LogoutAllDevices
{
    public class LogoutAllDevicesHandlerTests
    {
        private readonly Mock<IApplicationDbContext> _mockContext;
        private readonly Mock<ISessionService> _mockSessionService;
        private readonly Mock<ILogger<LogoutAllDevicesHandler>> _mockLogger;
        private readonly LogoutAllDevicesHandler _handler;

        public LogoutAllDevicesHandlerTests()
        {
            _mockContext = new Mock<IApplicationDbContext>();
            _mockSessionService = new Mock<ISessionService>();
            _mockLogger = new Mock<ILogger<LogoutAllDevicesHandler>>();

            _handler = new LogoutAllDevicesHandler(
                _mockContext.Object,
                _mockSessionService.Object,
                _mockLogger.Object
            );

            // Mock DbSet for Sessions
            var sessionsData = new List<Session>().AsQueryable();
            var mockSessionDbSet = new Mock<DbSet<Session>>();
            mockSessionDbSet.As<IQueryable<Session>>().Setup(m => m.Provider).Returns(sessionsData.Provider);
            mockSessionDbSet.As<IQueryable<Session>>().Setup(m => m.Expression).Returns(sessionsData.Expression);
            mockSessionDbSet.As<IQueryable<Session>>().Setup(m => m.ElementType).Returns(sessionsData.ElementType);
            mockSessionDbSet.As<IQueryable<Session>>().Setup(m => m.GetEnumerator()).Returns(() => sessionsData.GetEnumerator());

            // Setup for async operations like FirstOrDefaultAsync, CountAsync
            mockSessionDbSet.As<IAsyncEnumerable<Session>>()
                .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
                .Returns(new TestAsyncEnumerator<Session>(sessionsData.GetEnumerator()));

            _mockContext.Setup(c => c.Sessions).Returns(mockSessionDbSet.Object);
        }

        private LogoutAllDevicesCommand CreateLogoutAllDevicesCommand(string currentSessionToken = "current_session_token")
        {
            return new LogoutAllDevicesCommand
            {
                CurrentSessionToken = currentSessionToken,
                IpAddress = "127.0.0.1",
                UserAgent = "TestAgent/1.0"
            };
        }

        private User CreateStubUser(Guid? id = null, string email = "test@example.com")
        {
            var user = User.CreateNewUser(new Email(email), "passwordHash", "FirstName", "LastName");
            user.Id = id ?? Guid.NewGuid();
            return user;
        }

        private Session CreateTestSession(string token, User user, SessionStatus status = SessionStatus.Active)
        {
             var session = Session.CreateNewSession(
                user,
                new DeviceInfo { Browser = "Test", OperatingSystem = "TestOS", UserAgent = "TestAgent", DeviceFingerprint = "fp" },
                new LocationInfo { IpAddress = "127.0.0.1", City = "Test", Country = "Test" },
                "Password"
            );
            session.SessionToken = token;
            session.Status = status;
            session.UserId = user.Id; // Ensure UserId is set
            session.User = user;     // Ensure User navigation property is set
            return session;
        }

        [Fact]
        public async Task Handle_ValidCurrentSessionToken_ShouldRevokeAllUserSessionsAndReturnSuccess()
        {
            // Arrange
            var command = CreateLogoutAllDevicesCommand();
            var userId = Guid.NewGuid();
            var user = CreateStubUser(id: userId);
            var currentSession = CreateTestSession(command.CurrentSessionToken, user, SessionStatus.Active);

            var userSessions = new List<Session>
            {
                currentSession,
                CreateTestSession("other_token_1", user, SessionStatus.Active),
                CreateTestSession("other_token_2", user, SessionStatus.Expired) // This one should not be counted by the handler's logic
            };

            // Mock FirstOrDefaultAsync for current session
            _mockContext.Setup(c => c.Sessions.Include(s => s.User).FirstOrDefaultAsync(
                It.Is<Expression<Func<Session, bool>>>(expr => expr.Compile().Invoke(currentSession)), // Ensure the expression matches
                It.IsAny<CancellationToken>()
            )).ReturnsAsync(currentSession);


            // Mock CountAsync for active sessions
            // This is tricky because the actual LINQ query in the handler is _context.Sessions.Where(...).CountAsync()
            // We need to ensure our mock setup for _mockContext.Sessions can handle this.
            // A simpler way for unit tests is often to mock the direct result of such aggregate calls if possible,
            // or ensure the IQueryable setup correctly handles .Where and .CountAsync.
            // For this example, we'll rely on the handler's logic and mock the RevokeAllUserSessionsAsync directly.
            // The handler itself calculates activeSessions count before calling RevokeAllUserSessionsAsync.

            // Let's refine the mock for Sessions to allow proper filtering for CountAsync
            var sessionsQueryable = userSessions.AsQueryable();
            var mockSessionDbSet = new Mock<DbSet<Session>>();
            mockSessionDbSet.As<IQueryable<Session>>().Setup(m => m.Provider).Returns(new TestAsyncQueryProvider<Session>(sessionsQueryable.Provider));
            mockSessionDbSet.As<IQueryable<Session>>().Setup(m => m.Expression).Returns(sessionsQueryable.Expression);
            mockSessionDbSet.As<IQueryable<Session>>().Setup(m => m.ElementType).Returns(sessionsQueryable.ElementType);
            mockSessionDbSet.As<IQueryable<Session>>().Setup(m => m.GetEnumerator()).Returns(() => sessionsQueryable.GetEnumerator());
            mockSessionDbSet.As<IAsyncEnumerable<Session>>()
                .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
                .Returns(new TestAsyncEnumerator<Session>(sessionsQueryable.GetEnumerator()));
            _mockContext.Setup(c => c.Sessions).Returns(mockSessionDbSet.Object);


            _mockSessionService.Setup(s => s.RevokeAllUserSessionsAsync(userId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Todas las sesiones han sido cerradas exitosamente", result.Message);
            // The handler counts active sessions: currentSession + other_token_1 = 2
            Assert.Equal(2, result.SessionsTerminated);
            _mockSessionService.Verify(s => s.RevokeAllUserSessionsAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_InvalidCurrentSessionToken_ShouldReturnNotSuccess()
        {
            // Arrange
            var command = CreateLogoutAllDevicesCommand("invalid_token");
            _mockContext.Setup(c => c.Sessions.Include(s => s.User).FirstOrDefaultAsync(It.IsAny<Expression<Func<Session, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Session?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Sesión actual no encontrada", result.Message);
            Assert.Equal(0, result.SessionsTerminated);
            _mockSessionService.Verify(s => s.RevokeAllUserSessionsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_CurrentSessionNotActive_ShouldReturnNotSuccess()
        {
            // Arrange
            var command = CreateLogoutAllDevicesCommand();
            var user = CreateStubUser();
            var currentSession = CreateTestSession(command.CurrentSessionToken, user, SessionStatus.Revoked); // Not active

             _mockContext.Setup(c => c.Sessions.Include(s => s.User).FirstOrDefaultAsync(
                It.Is<Expression<Func<Session, bool>>>(expr => expr.Compile().Invoke(currentSession)),
                It.IsAny<CancellationToken>()
            )).ReturnsAsync(currentSession);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("La sesión actual no está activa", result.Message);
            Assert.Equal(0, result.SessionsTerminated);
            _mockSessionService.Verify(s => s.RevokeAllUserSessionsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }


    // Helper classes for mocking IAsyncEnumerable and IQueryable for EF Core operations in tests
    internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;

        public TestAsyncEnumerator(IEnumerator<T> inner)
        {
            _inner = inner;
        }

        public T Current => _inner.Current;

        public ValueTask DisposeAsync()
        {
            _inner.Dispose();
            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> MoveNextAsync()
        {
            return new ValueTask<bool>(_inner.MoveNext());
        }
    }

    internal class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
    {
        private readonly IQueryProvider _inner;

        internal TestAsyncQueryProvider(IQueryProvider inner)
        {
            _inner = inner;
        }

        public IQueryable CreateQuery(Expression expression)
        {
            return new TestAsyncEnumerable<TEntity>(expression);
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new TestAsyncEnumerable<TElement>(expression);
        }

        public object Execute(Expression expression)
        {
            return _inner.Execute(expression);
        }

        public TResult Execute<TResult>(Expression expression)
        {
            return _inner.Execute<TResult>(expression);
        }

        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
        {
            // This is a simplified version. For full EF Core async operations like CountAsync, SumAsync, etc.,
            // you might need a more sophisticated mock or rely on an in-memory provider for testing.
            // For CountAsync specifically, if it's the final operation, it might fall back to synchronous execution in some test setups.
            // The Execute or Execute<TResult> might be called by EF Core's internals.
            var expectedType = typeof(TResult).GetGenericArguments()[0];
            var executionResult = _inner.Execute(expression);

            if (executionResult is IEnumerable<TEntity> list)
            {
                 return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))
                                .MakeGenericMethod(expectedType)
                                .Invoke(null, new object[] { list.Count() });
            }

            return Task.FromResult((TResult)executionResult);

        }

        IAsyncEnumerable<TResult> IAsyncQueryProvider.ExecuteAsync<TResult>(Expression expression)
        {
             return new TestAsyncEnumerable<TResult>(expression);
        }
    }

    internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
    {
        public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) { }
        public TestAsyncEnumerable(Expression expression) : base(expression) { }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
        }

        IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
    }
}
