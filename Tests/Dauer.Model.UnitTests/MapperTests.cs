using Dauer.UnitTests.Shared;
using NUnit.Framework;

namespace Dauer.Model.UnitTests
{
    public class MapperTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Map_MapsWorkoutToTcx()
        {

        }

        [Test]
        public void Map_MapsTcxToWorkout()
        {
            var xml = TcxFixtures.GetGpsWorkout();
            var db = Dauer.Data.Tcx.Reader.Read(xml);
            var workout = new Dauer.Data.Tcx.Mapper().Map(db);
        }

        [Test]
        public void Map_MapsWorkoutToFit()
        {
        }

        [Test]
        public async Task Map_MapsFitToWorkout()
        {
            const string source = @"..\..\..\..\data\devices\forerunner-945\sports\running\"
                + @"generic\2019-12-18\35min-easy-4x20s-strides\garmin-connect\activity.fit";

            var fit = await new Dauer.Data.Fit.Reader().ReadAsync(source);
            var workout = new Dauer.Data.Fit.Mapper().Map(fit);
        }
    }
}