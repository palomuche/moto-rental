namespace MotoRentalApi.Tests
{
    public class MotoControllerTests
    {
        private MotoController _controller;
        private ApiDbContext _context;
        private ILogger<MotoController> _logger;

        public MotoControllerTests()
        {
            // Set up an in-memory database context for the tests
            var options = new DbContextOptionsBuilder<ApiDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApiDbContext(options);

            // Insert some test data into the in-memory context
            _context.Motos.Add(new Moto { Id = 1, Plate = "ABC1234", Model = "Test 1", Year = "2001" });
            _context.Motos.Add(new Moto { Id = 2, Plate = "XYZ4567", Model = "Test 2", Year = "2002" });
            _context.SaveChanges();

            _logger = Mock.Of<ILogger<MotoController>>();
            _controller = new MotoController(_context, _logger);
        }

        [Fact(DisplayName = "Get without plate")]
        public void Get_Returns_All_Motos()
        {
            // Act
            var result = _controller.Get() as OkObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            Assert.Equal(2, (result.Value as List<Moto>).Count);
        }


        [Fact(DisplayName = "Get with plate")]
        public void Get_Returns_Moto_By_Plate()
        {
            // Arrange
            var plate = "ABC1234";

            // Act
            var result = _controller.Get(plate) as OkObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
        }

        [Fact(DisplayName = "Post")]
        public void Post_Creates_New_Moto()
        {
            // Arrange
            var moto = new Moto { Plate = "HIJ9876", Model = "Test 3", Year = "2003" };

            // Act
            var result = _controller.Post(moto) as CreatedAtActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(201, result.StatusCode);
        }

        [Fact(DisplayName = "Post returns BadRequest for invalid plate format")]
        public void Post_Returns_BadRequest_For_Invalid_Plate_Format()
        {
            // Arrange
            var moto = new Moto { Plate = "InvalidPlate", Model = "Test 3", Year = "2003" };

            // Act
            var result = _controller.Post(moto) as BadRequestObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal("Invalid plate format.", result.Value);
        }

        [Fact(DisplayName = "Post returns BadRequest for duplicate plate")]
        public void Post_Returns_BadRequest_For_Duplicate_Plate()
        {
            // Arrange
            var moto = new Moto { Plate = "ABC1234", Model = "Test 3", Year = "2003" };

            // Act
            var result = _controller.Post(moto) as BadRequestObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal("Plate already exists.", result.Value);
        }

        [Fact(DisplayName = "Put")]
        public void Put_Updates_Moto()
        {
            // Arrange
            var id = 1;
            var newPlate = "DEF7890";

            // Act
            var result = _controller.Put(id, newPlate) as NoContentResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(204, result.StatusCode);

            var updatedMoto = _context.Motos.Find(id);
            Assert.Equal(newPlate, updatedMoto.Plate);
        }

        [Fact(DisplayName = "Delete")]
        public void Delete_Removes_Moto()
        {
            // Arrange
            var id = 1;

            // Act
            var result = _controller.Delete(id) as NoContentResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(204, result.StatusCode);

            var deletedMoto = _context.Motos.Find(id);
            Assert.Null(deletedMoto);
        }
    }
}
