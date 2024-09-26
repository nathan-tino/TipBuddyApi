using AutoMapper;
using TipBuddyApi.Data;

namespace TipBuddyApi.Configuration
{
    public class MapperConfig : Profile
    {
        public MapperConfig()
        {
            CreateMap<Shift, Shift>();
        }
    }
}
