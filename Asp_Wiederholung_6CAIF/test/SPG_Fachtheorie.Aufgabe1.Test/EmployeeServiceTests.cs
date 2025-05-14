using Microsoft.EntityFrameworkCore;
using SPG_Fachtheorie.Aufgabe1.Commands;
using SPG_Fachtheorie.Aufgabe1.Infrastructure;
using SPG_Fachtheorie.Aufgabe1.Model;
using SPG_Fachtheorie.Aufgabe1.Services;
using System;
using System.Linq;
using Xunit;

namespace SPG_Fachtheorie.Aufgabe1.Test
{
    public class EmployeeServiceTests
    {
        private AppointmentContext GetEmptyDbContext()
        {
            var options = new DbContextOptionsBuilder()
                .UseSqlite(@"Data Source=cash.db")
                .Options;

            var db = new AppointmentContext(options);
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();
            return db;
        }
        [Fact]
        public void AddCashierSuccessTest()
        {
            // ARRANGE
            var db = GetEmptyDbContext();
            var service = new EmployeeService(db);
            var cmd = new NewCashierCommand(
                1001, "FN", "LN", new DateOnly(2000, 1, 1), 5000, null, "Wursttheke");

            // ACT
            service.AddCashier(cmd);

            // ASSERT
            db.ChangeTracker.Clear();
            var cashierFromDb = db.Cashiers.First();
            Assert.True(cashierFromDb.RegistrationNumber == 1001);
        }

        [Fact]
        public void AddManagerSuccessTest()
        {
            // ARRANGE
            var db = GetEmptyDbContext();
            var service = new EmployeeService(db);
            var cmd = new NewManagerCommand(
                1001, "FN", "LN", new DateOnly(2000, 1, 1), null, null, "SUV");

            // ACT
            service.AddManager(cmd);

            // ASSERT
            db.ChangeTracker.Clear();
            var managerFromDb = db.Managers.First();
            Assert.True(managerFromDb.RegistrationNumber == 1001);
        }

        [Fact]
        public void AddManagerThrowsExceptionIfManagerEarnsTooMuch()
        {
            // ARRANGE
            var db = GetEmptyDbContext();
            var service = new EmployeeService(db);
            var cmd = new NewManagerCommand(
                1001, "FN", "LN", new DateOnly(2010, 1, 1), 5000, null, "SUV");

            // ACT & ASSERT
            var e = Assert.Throws<EmployeeServiceException>(() => service.AddManager(cmd));
            Assert.True(e.Message == $"Invalid salary for Employee {cmd.LastName}.");
        }

        [Fact]
        public void AddManagerThrowsExceptionIf3ManagersPresentTest()
        {
            // ARRANGE
            var db = GetEmptyDbContext();
            var service = new EmployeeService(db);
            var manager1 = new Manager(
                2001, "FN1", "LN1", new DateOnly(2000, 1, 1), null, null, "SUV");
            var manager2 = new Manager(
                2002, "FN1", "LN1", new DateOnly(2000, 1, 1), null, null, "SUV");
            var manager3 = new Manager(
                2003, "FN1", "LN1", new DateOnly(2000, 1, 1), null, null, "SUV");
            db.Managers.AddRange(manager1, manager2, manager3);
            db.SaveChanges();
            var cmd = new NewManagerCommand(
                2004, "FN", "LN", new DateOnly(2000, 1, 1), null, null, "SUV");

            // ACT & ASSERT
            var e = Assert.Throws<EmployeeServiceException>(() => service.AddManager(cmd));
            Assert.True(e.Message == "Only 3 managers are allowed.");

        }

        // Ich übergebe eine falsche registrationNumber, aber ein richtiges LastUpdate Datum --> Manager not found.
        // Ich übergebe eine richtige registrationNumber, aber ein falsches LastUpdate Datum --> Manager has changed.
        // UpdateManagerThrowsEmployeeServiceExceptionTest(1001, "2025-04-23T13:00:00", "Manager not found")
        // UpdateManagerThrowsEmployeeServiceExceptionTest(2001, "2025-04-23T12:00:00", "Manager has changed.")
        [Theory]
        [InlineData(2001, "2025-04-23T13:00:00", null)]
        [InlineData(1001, "2025-04-23T13:00:00", "Manager not found.")]
        [InlineData(2001, "2025-04-23T12:00:00", "Manager has changed.")]
        public void UpdateManagerThrowsEmployeeServiceExceptionTest(
            int registrationNumber, string lastUpdateStr, string? errorMessage)
        {
            // ARRANGE
            var lastUpdate = DateTime.Parse(lastUpdateStr);
            var db = GetEmptyDbContext();
            var service = new EmployeeService(db);
            var manager = new Manager(
                2001, "FN1", "LN1", new DateOnly(2000, 1, 1), null, null, "SUV")
            { LastUpdate = new DateTime(2025, 4, 23, 13, 0, 0) };
            var cmd = new UpdateManagerCommand(registrationNumber, "FN", "LN", null, "SUV", lastUpdate);
            db.Managers.Add(manager);
            db.SaveChanges();

            // ACT & ASSERT
            if (errorMessage is not null)
            {
                var e = Assert.Throws<EmployeeServiceException>(() => service.UpdateManager(cmd));
                Assert.True(e.Message == errorMessage);
            }
            // Wenn die erwartete ErrorMessage null ist, führe ich die Methode aus und prüfe,
            // ob der Eintrag richtig ist (success test).
            // Wem es leichter fällt, der soll dafür einen eigenen SuccessTest mit [Fact] schreiben.
            else
            {
                service.UpdateManager(cmd);
                db.ChangeTracker.Clear();
                var managerFromDb = db.Managers.First();
                Assert.True(managerFromDb.LastName == "LN");
            }
        }
    }
}
