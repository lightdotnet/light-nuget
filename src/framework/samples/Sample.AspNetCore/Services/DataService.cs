using Light.Extensions.DependencyInjection;

namespace Sample.AspNetCore.Services
{
    public class DataService : ITransientDependency
    {
        public string NewId => "Data_Id";
    }

    public interface IData1Service : ITransientDependency
    {
        public string NewId { get; }
    }

    public class Data1Service : IData1Service, ITransientDependency
    {
        public virtual string NewId => "Data1_Id";
    }

    public interface IData2Service : IData1Service
    {
    }

    public interface IData3Service : IData1Service
    {
        public string NewId3 { get; }
    }

    public class Data2Service : Data1Service, IData2Service, IData3Service
    {
        public override string NewId => "Data2_Id";

        public string NewId3 => "Data3_Id";
    }
}
