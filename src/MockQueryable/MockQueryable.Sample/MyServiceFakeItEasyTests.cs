using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using MockQueryable.FakeItEasy;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MockQueryable.Sample;

[TestFixture]
public class MyServiceFakeItEasyTests
{
    private static readonly CultureInfo UsCultureInfo = new("en-US");

    [TestCase("AnyFirstName", "AnyExistLastName", "01/20/2012", "Users with DateOfBirth more than limit")]
    [TestCase("ExistFirstName", "AnyExistLastName", "02/20/2012", "User with FirstName already exist")]
    [TestCase("AnyFirstName", "ExistLastName", "01/20/2012", "User already exist")]
    public async Task CreateUserIfNotExist(string firstName, string lastName, DateTime dateOfBirth, string expectedError)
    {
        // arrange
        var userRepository = A.Fake<IUserRepository>();
        var service = new MyService(userRepository);
        var users = new List<UserEntity>
        {
            new() {LastName = "ExistLastName", DateOfBirth = DateTime.Parse("01/20/2012", UsCultureInfo.DateTimeFormat)},
            new() {FirstName = "ExistFirstName"},
            new() {DateOfBirth = DateTime.Parse("01/20/2012", UsCultureInfo.DateTimeFormat)},
            new() {DateOfBirth = DateTime.Parse("01/20/2012", UsCultureInfo.DateTimeFormat)},
            new() {DateOfBirth = DateTime.Parse("01/20/2012", UsCultureInfo.DateTimeFormat)},
        };

        // expect
        var mock = users.BuildMock();
        A.CallTo(() => userRepository.GetQueryable()).Returns(mock);

        // act
        var ex = await Assert.ThrowsAsync<ApplicationException>(() =>
            service.CreateUserIfNotExist(firstName, lastName, dateOfBirth));

        // assert
        Assert.That(expectedError, Is.EqualTo(ex.Message));
    }

    [TestCase("01/20/2012", "06/20/2018", 5)]
    [TestCase("01/20/2012", "06/20/2012", 4)]
    [TestCase("01/20/2012", "02/20/2012", 3)]
    [TestCase("01/20/2010", "02/20/2011", 0)]
    public async Task GetUserReports(DateTime from, DateTime to, int expectedCount)
    {
        //arrange
        var userRepository = A.Fake<IUserRepository>();
        var service = new MyService(userRepository);
        var users = TestDataHelper.CreateUserList();
        //expect
        var mock = users.BuildMock();
        A.CallTo(() => userRepository.GetQueryable()).Returns(mock);
        //act
        var result = await service.GetUserReports(from, to);
        //assert
        Assert.That(expectedCount, Is.EqualTo(result.Count));
    }

    [TestCase("01/20/2012", "06/20/2018", 5)]
    [TestCase("01/20/2012", "06/20/2012", 4)]
    [TestCase("01/20/2012", "02/20/2012", 3)]
    [TestCase("01/20/2010", "02/20/2011", 0)]
    public async Task GetUserReports_AutoMap(DateTime from, DateTime to, int expectedCount)
    {
        // arrange
        var userRepository = A.Fake<IUserRepository>();
        var service = new MyService(userRepository);
        var users = TestDataHelper.CreateUserList();
        // expect
        var mock = users.BuildMock();
        A.CallTo(() => userRepository.GetQueryable()).Returns(mock);

        // act
        var result = await service.GetUserReportsAutoMap(from, to);

        // assert
        Assert.That(expectedCount, Is.EqualTo(result.Count));
    }

    [TestCase("AnyFirstName", "AnyExistLastName", "01/20/2012", "Users with DateOfBirth more than limit")]
    [TestCase("ExistFirstName", "AnyExistLastName", "02/20/2012", "User with FirstName already exist")]
    [TestCase("AnyFirstName", "ExistLastName", "01/20/2012", "User already exist")]
    public async Task DbSetCreatedFromCollectionCreateUserIfNotExist(string firstName, string lastName, DateTime dateOfBirth, string expectedError)
    {
        // arrange
        var users = new List<UserEntity>
        {
            new() {LastName = "ExistLastName", DateOfBirth = DateTime.Parse("01/20/2012", UsCultureInfo.DateTimeFormat)},
            new() {FirstName = "ExistFirstName"},
            new() {DateOfBirth = DateTime.Parse("01/20/2012", UsCultureInfo.DateTimeFormat)},
            new() {DateOfBirth = DateTime.Parse("01/20/2012", UsCultureInfo.DateTimeFormat)},
            new() {DateOfBirth = DateTime.Parse("01/20/2012", UsCultureInfo.DateTimeFormat)},
        };
        var mock = users.BuildMockDbSet();
        var userRepository = new TestDbSetRepository(mock);
        var service = new MyService(userRepository);
        // act
        var ex = await Assert.ThrowsAsync<ApplicationException>(() =>
            service.CreateUserIfNotExist(firstName, lastName, dateOfBirth));
        // assert
        Assert.That(expectedError, Is.EqualTo(ex.Message));
    }

    [TestCase("AnyFirstName", "ExistLastName", "01/20/2012")]
    public async Task DbSetCreatedFromCollectionCreateUser1(string firstName, string lastName, DateTime dateOfBirth)
    {
        // arrange
        var userEntities = new List<UserEntity>();
        var mock = userEntities.BuildMockDbSet();
        A.CallTo(() => mock.AddAsync(A<UserEntity>._, A<CancellationToken>._))
            .ReturnsLazily(call =>
            {
                userEntities.Add((UserEntity)call.Arguments[0]);
                return default;
            });
        var userRepository = new TestDbSetRepository(mock);
        var service = new MyService(userRepository);

        // act
        await service.CreateUserIfNotExist(firstName, lastName, dateOfBirth);

        // assert
        var entity = await mock.SingleAsync();
        Assert.That(firstName, Is.EqualTo(entity.FirstName));
        Assert.That(lastName, Is.EqualTo(entity.LastName));
        Assert.That(dateOfBirth, Is.EqualTo(entity.DateOfBirth));
    }

    [TestCase("01/20/2012", "06/20/2018", 5)]
    [TestCase("01/20/2012", "06/20/2012", 4)]
    [TestCase("01/20/2012", "02/20/2012", 3)]
    [TestCase("01/20/2010", "02/20/2011", 0)]
    public async Task DbSetCreatedFromCollectionGetUserReports(DateTime from, DateTime to, int expectedCount)
    {
        // arrange
        var users = TestDataHelper.CreateUserList();
        var mock = users.BuildMockDbSet();
        var userRepository = new TestDbSetRepository(mock);
        var service = new MyService(userRepository);

        // act
        var result = await service.GetUserReports(from, to);

        // assert
        Assert.That(expectedCount, Is.EqualTo(result.Count));
    }

    [Test]
    public async Task DbSetCreatedFromCollectionGetAllUserEntitiesAsync()
    {
        // arrange
        var users = TestDataHelper.CreateUserList();

        var mockDbSet = users.BuildMockDbSet();
        var userRepository = new TestDbSetRepository(mockDbSet);

        // act
        var result = await userRepository.GetAllAsync().ToListAsync();

        // assert
        Assert.That(users.Count, Is.EqualTo(result.Count));
    }

    [Test]
    public async Task DbSetGetAllUserEntitiesAsync_ShouldReturnAllEntities_WhenSourceIsChanged()
    {
        // arrange
        var users = new List<UserEntity>();

        var mockDbSet = users.BuildMockDbSet();
        var userRepository = new TestDbSetRepository(mockDbSet);


        // act
        var result1 = await userRepository.GetAllAsync().ToListAsync();
        users.AddRange(TestDataHelper.CreateUserList());
        var result2 = await userRepository.GetAllAsync().ToListAsync();

        // assert
        Assert.That(0, Is.EqualTo(result1.Count));
        Assert.That(users.Count, Is.EqualTo(result2.Count));
    }

    [Test]
    public async Task DbSetCreatedFromCollectionGetAllUserEntity()
    {
        // arrange
        var users = TestDataHelper.CreateUserList();
        var mock = users.BuildMockDbSet();
        var userRepository = new TestDbSetRepository(mock);

        // act
        var result = await userRepository.GetAll();

        // assert
        Assert.That(users.Count, Is.EqualTo(result.Count));
    }

    [Test]
    public async Task GetUsersByFirstName_ExpressionVisitorMissing_ThrowsException()
    {
        // arrange
        var users = TestDataHelper.CreateUserList();

        var mockDbSet = users.BuildMockDbSet();
        var userRepository = new TestDbSetRepository(mockDbSet);

        // act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => userRepository.GetUsersByFirstName("naME"));

        // assert
        Assert.That(
            exception.Message,
            Is.EqualTo(
                "The 'Like' method is not supported because the query has switched to client-evaluation. " +
                "This usually happens when the arguments to the method cannot be translated to server. " +
                "Rewrite the query to avoid client evaluation of arguments so that method can be translated to server."));
    }

    [Test]
    public async Task GetUsersByFirstName_PartOfNameCaseInsensitiveSearch_AllMatchesReturned()
    {
        // arrange
        var users = TestDataHelper.CreateUserList();

        var mockDbSet = users.BuildMockDbSet<UserEntity, SampleLikeExpressionVisitor>();
        var userRepository = new TestDbSetRepository(mockDbSet);

        // act
        var result = await userRepository.GetUsersByFirstName("naME3");

        // assert
        Assert.That(result.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task GetUsersByLastName_ExpressionVisitorMissing_ThrowsException()
    {
        // arrange
        var users = TestDataHelper.CreateUserList();

        var mockDbSet = users.BuildMockDbSet();
        var userRepository = new TestDbSetRepository(mockDbSet);

        // act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => userRepository.GetUsersByLastName("naME4"));

        // assert
        Assert.That(
            exception.Message,
            Is.EqualTo(
                "The 'ILike' method is not supported because the query has switched to client-evaluation. " +
                "This usually happens when the arguments to the method cannot be translated to server. " +
                "Rewrite the query to avoid client evaluation of arguments so that method can be translated to server."));
    }

    [Test]
    public async Task GetUsersByLastName_PartOfNameCaseInsensitiveSearch_AllMatchesReturned()
    {
        // arrange
        var users = TestDataHelper.CreateUserList();

        var mockDbSet = users.BuildMockDbSet<UserEntity, SampleLikeExpressionVisitor>();
        var userRepository = new TestDbSetRepository(mockDbSet);

        // act
        var result = await userRepository.GetUsersByLastName("naME4");

        // assert
        Assert.That(result.Count, Is.EqualTo(3));
    }

    [Test]
    public async Task GetUsersCount_AsQueryable_CountMatchesReturned()
    {
        // arrange
        var userId = Guid.NewGuid();
        var users = TestDataHelper.CreateUserList(userId);

        var mockDbSet = users.BuildMockDbSet();
        var userRepository = new TestDbSetRepository(mockDbSet);

        // act
        var result = await userRepository.GetQueryable().Where(x=>x.Id != userId).CountAsync();

        // assert
        Assert.That(users.Count - 1, Is.EqualTo(result));
    }

    [Test]
    public async Task DbSetCreatedFromCollection_ExecuteDeleteAsync()
    {
        // arrange
        var userId = Guid.NewGuid();
        var users = TestDataHelper.CreateUserList(userId);

        var mockDbSet = users.BuildMockDbSet();
        var userRepository = new TestDbSetRepository(mockDbSet);

        // act
        var count = await userRepository.DeleteUserAsync(userId);

        // assert
        Assert.That(count, Is.EqualTo(1));
        var updatedUsers = await userRepository.GetAllAsync().ToListAsync();
        Assert.That(updatedUsers.Any(x => x.Id == userId), Is.EqualTo(false));
    }

    [Test]
    public async Task DbSetCreatedFromCollectionExecuteDeleteAsync_ShouldReturnZero()
    {
        // arrange
        var userId = Guid.NewGuid();
        var users = TestDataHelper.CreateUserList(userId);

        var mockDbSet = users.BuildMockDbSet();
        var userRepository = new TestDbSetRepository(mockDbSet);

        //act
        var count = await userRepository.DeleteUserAsync(Guid.NewGuid());

        // assert
        Assert.That(count, Is.EqualTo(0));
    }

    [Test]
    public async Task DbSetCreatedFromCollectionExecuteUpdateAsync()
    {
        // arrange
        var expectedName = "Unit Test";
        var userId = Guid.NewGuid();
        var users = TestDataHelper.CreateUserList(userId);

        var mockDbSet = users.BuildMockDbSet();
        var userRepository = new TestDbSetRepository(mockDbSet);

        //act
        var count = await userRepository.UpdateFirstAndLastNameByIdAsync(userId, expectedName);

        //assert
        Assert.That(count, Is.EqualTo(1));
        var user = users.Single(x => x.Id == userId);
        Assert.That(expectedName, Is.EqualTo(user.FirstName));
        Assert.That(expectedName, Is.EqualTo(user.LastName));
    }

    [Test]
    public async Task DbSetCreatedFromCollectionExecuteUpdateAsync_ShouldReturnZero()
    {
        // arrange
        var userId = Guid.NewGuid();
        var users = TestDataHelper.CreateUserList(userId);
        var arrangeUser = users.Single(x => x.Id == userId);
        var expectedFirstName = arrangeUser.FirstName;
        var expectedLastName = arrangeUser.LastName;
        var mockDbSet = users.BuildMockDbSet();
        var userRepository = new TestDbSetRepository(mockDbSet);

        //act
        var count = await userRepository.UpdateFirstAndLastNameByIdAsync(Guid.NewGuid(), "Unit Test");

        //assert
        Assert.That(count, Is.EqualTo(0));

        var user = users.Single(x => x.Id == userId);
        Assert.That(expectedFirstName, Is.EqualTo(user.FirstName));
        Assert.That(expectedLastName, Is.EqualTo(user.LastName));
    }

    [Test]
    public async Task DbSetValueExpressionExecuteUpdateAsync()
    {
        // arrange
        var userId = Guid.NewGuid();
        var users = TestDataHelper.CreateUserList(userId);
        var valueBeforeChange = users.Single(x => x.Id == userId).DateOfBirth;

        var mockDbSet = users.BuildMockDbSet();
        var userRepository = new TestDbSetRepository(mockDbSet);

        //act
        var count = await userRepository.AddYearToDateOfBirth(userId);

        //assert
        Assert.That(count, Is.EqualTo(1));
        var user = users.Single(x => x.Id == userId);
        Assert.That(valueBeforeChange.AddYears(1), Is.EqualTo(user.DateOfBirth));

    }


    [Test]
    public async Task DbSetValueSetDateOfBirthExecuteUpdateAsync()
    {
        // arrange
        var userId = Guid.NewGuid();
        var users = TestDataHelper.CreateUserList(userId);
        var expectedYear = DateTime.Now.Year + 42;

        var mockDbSet = users.BuildMockDbSet();
        var dbset = mockDbSet;

        //act
        var count = await dbset.Where(x => x.Id == userId)
            .ExecuteUpdateAsync(opt => opt.SetProperty(x => x.DateOfBirth, x => new DateTime(expectedYear, 10, 14, 0, 0, 0)));

        //assert
        Assert.That(count, Is.EqualTo(1));
        var user = users.Single(x => x.Id == userId);
        Assert.That(expectedYear, Is.EqualTo(user.DateOfBirth.Year));

    }
}